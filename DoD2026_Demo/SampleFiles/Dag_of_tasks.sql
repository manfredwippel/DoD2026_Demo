/*
Create a DAG of tasks 
When condition to run a task
*/

USE SCHEMA DOD2026_DB.DOD2026_DEMO;

USE ROLE ACCOUNTADMIN;



--Same table as in the previous lecture
CREATE OR REPLACE TABLE DOD2026_DB.DOD2026_DEMO.prices (
    id INT,
    price NUMBER,
    insert_time TIME,
    insert_task VARCHAR
);

SELECT *
FROM DOD2026_DB.DOD2026_DEMO.prices;


--Our Stored Procedure
CREATE OR REPLACE PROCEDURE INSERT_PRICE_ROWS(N NUMBER, TASK_NAME TEXT)
RETURNS VARCHAR
LANGUAGE SQL
AS 
$$
DECLARE
    ID NUMBER;
    PRICE NUMBER;
    COUNTER NUMBER;
BEGIN
    COUNTER := 1;
    WHILE (COUNTER<=N) DO
        ID := UNIFORM(1, 100, random());
        PRICE := UNIFORM(1, 100, random()) * 100;
        INSERT INTO DOD2026_DB.DOD2026_DEMO.prices (ID, PRICE, INSERT_TIME, INSERT_TASK)
        VALUES (:ID, :PRICE, CURRENT_TIME(), :TASK_NAME);
        COUNTER := COUNTER + 1;
    END WHILE;
    RETURN 'Completed';
END
$$;


SHOW TASKS;

SHOW PROCEDURES;


--Root task
CREATE OR REPLACE TASK task_1
WAREHOUSE = DOD_WAREHOUSE
SCHEDULE = '1 minute'
AS
CALL INSERT_PRICE_ROWS(1, 'task_1');



--First child task 
CREATE OR REPLACE TASK task_2
WAREHOUSE = DOD_WAREHOUSE
AFTER task_1
AS
CALL INSERT_PRICE_ROWS(3, 'task_2');




--Second child task 
CREATE OR REPLACE TASK task_3
WAREHOUSE = DOD_WAREHOUSE
AFTER task_1
AS
CALL INSERT_PRICE_ROWS(5, 'task_3');



SELECT *
FROM DOD2026_DB.DOD2026_DEMO.prices
ORDER BY insert_time;

SHOW
TASKS;







--Last task with 2 predecessors
CREATE OR REPLACE TASK task_4
WAREHOUSE = DOD_WAREHOUSE
AFTER task_2, task_3
AS
CALL INSERT_PRICE_ROWS(1, 'task_4');


--Root task must be suspended to update a DAG




--First the child tasks should be resumed
ALTER TASK task_2 RESUME;
ALTER TASK task_3 RESUME;
ALTER TASK task_4 RESUME;

--Then the root task
ALTER TASK task_1 RESUME;



SHOW TASKS;


--Truncate to test a new round
TRUNCATE TABLE DOD2026_DB.DOD2026_DEMO.prices;


--Validate
SHOW TASKS;



SELECT *
FROM DOD2026_DB.DOD2026_DEMO.prices
ORDER BY insert_time;


--Task history
SELECT *
FROM TABLE(information_schema.task_history(
        scheduled_time_range_start => DATEADD('hour', -1, CURRENT_TIMESTAMP()),
        result_limit => 20)
     )
WHERE LOWER(name) IN ('task_1', 'task_2', 'task_3', 'task_4')
ORDER BY scheduled_time DESC;


--Suspend all 4
ALTER TASK task_1 SUSPEND;
ALTER TASK task_2 SUSPEND;
ALTER TASK task_3 SUSPEND;
ALTER TASK task_4 SUSPEND;

--To get the list of direct child tasks
SELECT *
FROM TABLE(INFORMATION_SCHEMA.TASK_DEPENDENTS(task_name => 'task_3'));


SELECT *
FROM TABLE(INFORMATION_SCHEMA.TASK_DEPENDENTS(task_name => 'task_1', recursive => FALSE));


--Condition to run a task

--We will use this in the Stream's section
CREATE OR REPLACE TASK task_4
WAREHOUSE = DOD_WAREHOUSE
AFTER task_2, task_3
WHEN 1=2--SYSTEM$STREAM_HAS_DATA ('stream_name')
AS
CALL INSERT_PRICE_ROWS(1, 'task_4')
;


--Validate
SHOW TASKS;

TRUNCATE TABLE DOD2026_DB.DOD2026_DEMO.prices;

SELECT *
FROM DOD2026_DB.DOD2026_DEMO.prices;


--Cleanup (important!)

--Drop all tasks (or at least suspend them)
DROP TASK task_1;
DROP TASK task_2;
DROP TASK task_3;
DROP TASK task_4;

DROP TABLE DOD2026_DB.DOD2026_DEMO.prices;


DROP PROCEDURE IF EXISTS INSERT_PRICE_ROWS(INT);
DROP PROCEDURE IF EXISTS INSERT_PRICE_ROWS(INT, VARCHAR);
