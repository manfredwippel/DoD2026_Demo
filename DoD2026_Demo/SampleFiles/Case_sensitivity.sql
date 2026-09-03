-- DDL statements manipulate database objects.

--DATABASE
--SCHEMA
--TABLE
--VIEW
--TASK
--STORED PROCEDURE
--ROLE
--USER
--STAGE
--STREAM
--SHARE
--FILE FORMAT

-- Use table objects to demonstrate DDL statements.

CREATE DATABASE IF NOT EXISTS DDL_DEMO;



CREATE SCHEMA DDL_DEMO.sql;


--2 main ways:
--1. From scratch
--2. From an existing object

--1. From scratch
CREATE TABLE DDL_DEMO.sql.orders
(O_ORDERKEY NUMBER(38,0),
O_CUSTKEY NUMBER(38,0),
O_ORDERSTATUS VARCHAR(1),
O_TOTALPRICE NUMBER(12,2),
O_ORDERDATE DATE,
O_ORDERPRIORITY VARCHAR(15),
O_CLERK VARCHAR(15),
O_SHIPPRIORITY NUMBER(38,0),
O_COMMENT VARCHAR)
;



/* 
Fully qualified object names:
database_name.schema_name.object_name
*/


SELECT *
FROM orders;




/* 
SHOW: provides info on database objects
DESC (DESCRIBE): provides detailed info on an object
*/



SHOW TABLES IN DDL_DEMO.SQL;


DESC TABLE DDL_DEMO.sql.orders;


/* 
GET_DDL is very helpful
*/
SELECT GET_DDL('TABLE', 'DDL_DEMO.sql.orders');



-- A small DML example.
INSERT INTO 
DDL_DEMO.sql.orders
VALUES
(3011172,65,'O',67802.72,'1997-07-14','5-LOW','Clerk#000000926',0,'foxes cajole bold deposits. furiously even Tiresias wake carefully. special in'),
(3012964,23,'F',134155.09,'1993-06-22','4-NOT SPECIFIED','Clerk#000000201',0,'ccounts could are quickly. regular dep'),
(3017056,40,'F',7438.41,'1994-01-08','2-HIGH','Clerk#000000477',0,'ges. furiously express instructions from the spec')
;


-- Validate the result.
SELECT *
FROM DDL_DEMO.sql.orders;



--ALTER, add one column
ALTER TABLE DDL_DEMO.sql.orders
ADD COLUMN NEW_COL_1 VARCHAR;




--Add multiple columns
ALTER TABLE DDL_DEMO.sql.orders
ADD COLUMN 
NEW_COL_2 VARCHAR,
NEW_COL_3 VARCHAR;




--DROP TABLE
DROP TABLE DDL_DEMO.sql.orders;



--UNDROP TABLE
UNDROP TABLE DDL_DEMO.sql.orders;




--2. From existing objects

--CREATE TABLE AS (CTAS)
CREATE TABLE DDL_DEMO.sql.orders_ctas
AS
SELECT * FROM SNOWFLAKE_SAMPLE_DATA.TPCH_SF1.ORDERS LIMIT 10;


--Validate
SELECT *
FROM DDL_DEMO.sql.orders_ctas;



SELECT GET_DDL('table', 'DDL_DEMO.sql.orders_ctas');




--CREATE TABLE LIKE
CREATE TABLE DDL_DEMO.sql.orders_like
LIKE SNOWFLAKE_SAMPLE_DATA.TPCH_SF1.ORDERS;


--Validate
SELECT *
FROM DDL_DEMO.sql.orders_like;








--CREATE TABLE CLONE
CREATE TABLE DDL_DEMO.sql.orders_clone
CLONE DDL_DEMO.sql.orders_ctas;
-- Zero copy cloning!!

--Validate
SELECT *
FROM DDL_DEMO.sql.orders_clone
limit 100;

SELECT GET_DDL('TABLE', 'DDL_DEMO.sql.orders_clone');



-- A table cannot be cloned from a share.
CREATE TABLE DDL_DEMO.sql.orders_clone_2
CLONE SNOWFLAKE_SAMPLE_DATA.TPCH_SF1.ORDERS;




/* Comments

For
1. The table
2. Specific column

*/

--1. Comment on table
CREATE OR REPLACE TABLE DDL_DEMO.sql.orders
(O_ORDERKEY NUMBER(38,0),
O_CUSTKEY NUMBER(38,0),
O_ORDERSTATUS VARCHAR(1),
O_TOTALPRICE NUMBER(12,2),
O_ORDERDATE DATE,
O_ORDERPRIORITY VARCHAR(15),
O_CLERK VARCHAR(15),
O_SHIPPRIORITY NUMBER(38,0),
O_COMMENT VARCHAR)
COMMENT='Orders fact table that gets updated daily.'
;


--Shows the table comment
SHOW TABLES;


--Won't show the table comment
DESC TABLE DDL_DEMO.sql.orders;


--2. Comment on a specific column
CREATE OR REPLACE TABLE DDL_DEMO.sql.orders
(O_ORDERKEY NUMBER(38,0),
O_CUSTKEY NUMBER(38,0),
O_ORDERSTATUS VARCHAR(1),
O_TOTALPRICE NUMBER(12,2),
O_ORDERDATE DATE COMMENT 'When the payment was made.',
O_ORDERPRIORITY VARCHAR(15),
O_CLERK VARCHAR(15),
O_SHIPPRIORITY NUMBER(38,0),
O_COMMENT VARCHAR)
COMMENT='Orders fact table that gets updated daily.' --Notice the syntactical difference
;







/* Few best practices */
--1. Use "IF NOT EXISTS". Good practice to avoid overwriting objects.
CREATE TABLE IF NOT EXISTS DDL_DEMO.sql.orders
(O_ORDERKEY NUMBER(38,0),
O_CUSTKEY NUMBER(38,0),
O_ORDERSTATUS VARCHAR(1),
O_TOTALPRICE NUMBER(12,2),
O_ORDERDATE DATE,
O_ORDERPRIORITY VARCHAR(15),
O_CLERK VARCHAR(15),
O_SHIPPRIORITY NUMBER(38,0),
O_COMMENT VARCHAR)
;


-- 2. If overwrite is not a concern, use "CREATE OR REPLACE"
CREATE OR REPLACE TABLE DDL_DEMO.sql.orders
(O_ORDERKEY NUMBER(38,0),
O_CUSTKEY NUMBER(38,0),
O_ORDERSTATUS VARCHAR(1),
O_TOTALPRICE NUMBER(12,2),
O_ORDERDATE DATE,
O_ORDERPRIORITY VARCHAR(15),
O_CLERK VARCHAR(15),
O_SHIPPRIORITY NUMBER(38,0),
O_COMMENT VARCHAR)
;


--3. Be aware of the case sensitivity in object names.

--Object names are internally stored in UPPERCASE

CREATE OR REPLACE TABLE case_sensitivity
(ID INT);


SHOW TABLES IN DDL_DEMO.SQL;

--This is how the DDL is parsed
SELECT GET_DDL('table', 'case_sensitivity');


CREATE OR REPLACE TABLE case_SENsitiviTY
(ID INT)
;



--To enforce case sensitivity use double quotes ""
CREATE TABLE "case_SENsitiviTY"
(ID INT)
;



SHOW TABLES IN DDL_DEMO.SQL;


DROP TABLE case_SENsitiviTY; --will not drop case_SENsitiviTY


--The reason is case_SENsitiviTY is converted to uppercase during parsing
DROP TABLE "case_SENsitiviTY";



--4. Note the difference between " and '
--"": object identifier
--'': string literal


--Example 1
CREATE OR REPLACE TABLE "case_SENsitiviTY"
(ID INT)
;



-- Example 2
SHOW TABLES IN DDL_DEMO.SQL;

SELECT GET_DDL('table', '"case_SENsitiviTY"');



--Example 3
CREATE OR REPLACE TABLE "case SENsitiviTY"
("My ID" INT)
;

SELECT GET_DDL('TABLE', '"case SENsitiviTY"');

/* Cleanup */
SHOW TABLES IN DDL_DEMO.SQL;

DROP DATABASE DDL_DEMO;
