USE SCHEMA DOD2026_DB.DOD2026_DEMO;
USE WAREHOUSE DOD_WAREHOUSE;

/*
Create tasks to insert rows
Scheduling tasks
Learn cron expressions
Create serverless tasks
Trouble-shooting tasks using task history
Create a stored procedure and call that by a task

*/

CREATE OR REPLACE TABLE DOD2026_DB.DOD2026_DEMO.prices
(
    id          INT,
    price       NUMBER,
    insert_time TIME,
    task_name   VARCHAR
);


--A sample insert
INSERT INTO prices
VALUES (10, 200, current_time(), 'no task');



SELECT *
FROM prices;



TRUNCATE TABLE prices;


--Basic task syntax
CREATE OR REPLACE TASK task_1
    WAREHOUSE = DOD_WAREHOUSE
    SCHEDULE = '1 minute'
    AS
        INSERT INTO DOD2026_DB.DOD2026_DEMO.prices --fully qualified name
        VALUES (10, 200, current_time(), 'task_1')
;


--A few points
--1. Fully qualified name

--2. No temporary table

--3. New tasks are created as SUSPENDED. Alter task to RESUME to work.
SHOW TASKS;


--4. The role that creates task needs EXECUTE TASK privilege

ALTER TASK task_1 RESUME;

EXECUTE TASK task_1;


--Grant execute task privilege to sysadmin

USE ROLE ACCOUNTADMIN;

SHOW GRANTS TO ROLE SYSADMIN;

GRANT EXECUTE TASK ON ACCOUNT TO ROLE SYSADMIN;

--REVOKE EXECUTE TASK ON ACCOUNT FROM ROLE sysadmin;

SHOW GRANTS TO ROLE SYSADMIN;

USE ROLE SYSADMIN;


--Execute the task manually
EXECUTE TASK task_1;



SELECT *
FROM prices;



SHOW TASKS;


--Start the task
ALTER TASK task_1 RESUME;


--Validate
SELECT *
FROM prices;


--Task history
SELECT *
FROM TABLE (information_schema.task_history(
        scheduled_time_range_start => dateadd('minute', -30, current_timestamp()),
        result_limit => 10,
        task_name => 'task_1'))
ORDER BY scheduled_time DESC;



ALTER TASK task_1 SUSPEND;


--Using CRON
CREATE OR REPLACE TASK task_1
    WAREHOUSE = DOD_WAREHOUSE
    SCHEDULE = 'USING CRON * * * * * Europe/London' --USING CRON + cron expression + timezone
    AS
        INSERT INTO DOD2026_DB.DOD2026_DEMO.prices
        VALUES (10, 200, current_time(), 'task_1')
;


--Validate
TRUNCATE TABLE prices;

SELECT *
FROM prices;

ALTER TASK task_1 RESUME;



/*
Using CRon

# __________ minute (0-59)
# | ________ hour (0-23)
# | | ______ day of month (1-31, or L)
# | | | ____ month (1-12, JAN-DEC)
# | | | | _ day of week (0-6, SUN-SAT, or L)
# | | | | |
# | | | | |
  * * * * *

from Snowflake documentation

'* * * * *' --> run at every minute
'0 * * * *' --> run at minute 0 every hour
'0 6 * * *' --> run at 6:00AM of every day
'0 6 15 * *' --> run at 6:00AM of day 15th of every month
*/


--Serverless

--First suspend the existing task
ALTER TASK task_1 SUSPEND;


DROP TASK task_1;

USE ROLE ACCOUNTADMIN;

--USING CRON + cron expression + timezone
CREATE OR REPLACE TASK task_1
    SCHEDULE = 'USING CRON * * * * * Europe/London'
    AS
        INSERT INTO DOD2026_DB.DOD2026_DEMO.prices
        VALUES (10, 200, current_time(), 'task_1')
;

-- the same using the simpler syntax
CREATE OR REPLACE TASK task_1
    SCHEDULE = '1 MINUTES'
    AS
        INSERT INTO DOD2026_DB.DOD2026_DEMO.prices
        VALUES (10, 200, current_time(), 'task_1')
;

--Validate
TRUNCATE TABLE prices;

SELECT *
FROM prices;

ALTER TASK task_1 RESUME;


DROP TASK task_1;
USE ROLE SYSADMIN;


/* Create task to call a stored procedure */


--Create stored procedure
CREATE OR REPLACE PROCEDURE INSERT_PRICE_ROWS(N NUMBER)
    RETURNS VARCHAR
    LANGUAGE SQL
AS
$$
DECLARE
    ID      NUMBER;
    PRICE   NUMBER;
    COUNTER NUMBER;
BEGIN
    COUNTER := 1;
    WHILE (COUNTER <= N)
        DO
            ID := UNIFORM(1, 100, random());
            PRICE := UNIFORM(1, 100, random()) * 100;
            INSERT INTO prices (ID, PRICE, INSERT_TIME)
            VALUES (:ID, :PRICE, CURRENT_TIME());
            COUNTER := COUNTER + 1;
        END WHILE;
    RETURN 'Completed';
END
$$;


--Task with calling the stored procedure
CREATE OR REPLACE TASK task_1
    WAREHOUSE = DOD_WAREHOUSE
    SCHEDULE = '1 minute'
    AS
        CALL INSERT_PRICE_ROWS(3)
;

--Validate
TRUNCATE TABLE prices;

SELECT *
FROM prices;

ALTER TASK task_1 RESUME;


--Task history
SELECT *
FROM TABLE (information_schema.task_history(
        scheduled_time_range_start => dateadd('minute', -30, current_timestamp()),
        result_limit => 10,
        task_name => 'task_1'))
ORDER BY scheduled_time DESC;


-- Cleanup (important).

ALTER TASK task_1 SUSPEND;

DROP TASK task_1;

-- REVOKE EXECUTE TASK ON ACCOUNT FROM ROLE SYSADMIN;
