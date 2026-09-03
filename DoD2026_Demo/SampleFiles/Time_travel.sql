-- Snowflake Time Travel demo
-- Run the script statement by statement or run it in full without editing.
-- An active warehouse and a role that can create databases are required.

-- Start with a clean, isolated demo environment.

CREATE DATABASE IF NOT EXISTS DOD2026_TIME_TRAVEL_DEMO
	DATA_RETENTION_TIME_IN_DAYS = 1;

CREATE SCHEMA IF NOT EXISTS DOD2026_TIME_TRAVEL_DEMO.SALES;

USE SCHEMA DOD2026_TIME_TRAVEL_DEMO.SALES;

CREATE OR REPLACE TABLE ORDERS (
	ORDER_ID NUMBER NOT NULL,
	CUSTOMER_NAME VARCHAR NOT NULL,
	PRODUCT_NAME VARCHAR NOT NULL,
	QUANTITY NUMBER NOT NULL,
	UNIT_PRICE NUMBER(10, 2) NOT NULL,
	STATUS VARCHAR NOT NULL,
	ORDER_DATE DATE NOT NULL
);

INSERT INTO ORDERS
	(ORDER_ID, CUSTOMER_NAME, PRODUCT_NAME, QUANTITY, UNIT_PRICE, STATUS, ORDER_DATE)
VALUES
	(1001, 'Ada Lovelace', 'Mechanical Keyboard', 1, 129.00, 'NEW', '2026-01-12'),
	(1002, 'Grace Hopper', 'USB-C Dock', 2, 89.50, 'NEW', '2026-01-13'),
	(1003, 'Alan Turing', '4K Monitor', 1, 399.00, 'NEW', '2026-01-14'),
	(1004, 'Margaret Hamilton', 'Webcam', 3, 79.00, 'SHIPPED', '2026-01-15');

-- The table starts with four orders.
SELECT *
FROM ORDERS
ORDER BY ORDER_ID;

-- Time Travel is enabled through the retention period.
SHOW TABLES LIKE 'ORDERS';

-- Capture the initial state by timestamp. The pause also makes a negative
-- offset safe to query when the complete script is run in one operation.
SET BEFORE_UPDATE_TIMESTAMP = CURRENT_TIMESTAMP();

CALL SYSTEM$WAIT(5);

-- Simulate an accidental bulk update and capture its query ID.
UPDATE ORDERS
SET STATUS = 'CANCELLED'
WHERE TRUE;

SET UPDATE_QUERY_ID = LAST_QUERY_ID();

-- Current data contains the accidental change.
SELECT *
FROM ORDERS
ORDER BY ORDER_ID;

-- Query the table at the timestamp captured before the update.
SELECT *
FROM ORDERS AT (TIMESTAMP => $BEFORE_UPDATE_TIMESTAMP)
ORDER BY ORDER_ID;

-- OFFSET is expressed in seconds relative to the query start time.
SELECT *
FROM ORDERS AT (OFFSET => -5)
ORDER BY ORDER_ID;

-- BEFORE excludes the changes made by the captured statement.
SELECT *
FROM ORDERS BEFORE (STATEMENT => $UPDATE_QUERY_ID)
ORDER BY ORDER_ID;

-- AT includes the changes made by the captured statement.
SELECT *
FROM ORDERS AT (STATEMENT => $UPDATE_QUERY_ID)
ORDER BY ORDER_ID;

-- Restore the original status values from the historical table state.
UPDATE ORDERS AS CURRENT_ORDERS
SET STATUS = HISTORICAL_ORDERS.STATUS
FROM ORDERS BEFORE (STATEMENT => $UPDATE_QUERY_ID) AS HISTORICAL_ORDERS
WHERE CURRENT_ORDERS.ORDER_ID = HISTORICAL_ORDERS.ORDER_ID;

SELECT *
FROM ORDERS
ORDER BY ORDER_ID;

-- Simulate an accidental deletion and capture its query ID.
DELETE FROM ORDERS
WHERE ORDER_ID = 1003;

SET DELETE_QUERY_ID = LAST_QUERY_ID();

-- The deleted order is absent from the current table.
SELECT *
FROM ORDERS
ORDER BY ORDER_ID;

-- It is still visible immediately before the DELETE statement.
SELECT *
FROM ORDERS BEFORE (STATEMENT => $DELETE_QUERY_ID)
ORDER BY ORDER_ID;

-- Restore only the deleted row from Time Travel.
INSERT INTO ORDERS
SELECT *
FROM ORDERS BEFORE (STATEMENT => $DELETE_QUERY_ID)
WHERE ORDER_ID = 1003;

SELECT *
FROM ORDERS
ORDER BY ORDER_ID;

-- Create an instant zero-copy clone of the table as it existed before deletion.
CREATE TABLE ORDERS_BEFORE_DELETE
	CLONE ORDERS BEFORE (STATEMENT => $DELETE_QUERY_ID);

SELECT *
FROM ORDERS_BEFORE_DELETE
ORDER BY ORDER_ID;

-- Dropped objects remain recoverable during the retention period.
DROP TABLE ORDERS_BEFORE_DELETE;

UNDROP TABLE ORDERS_BEFORE_DELETE;

SELECT *
FROM ORDERS_BEFORE_DELETE
ORDER BY ORDER_ID;

-- A complete database can also be restored within the retention period.
CREATE DATABASE RESTORED_DB CLONE DOD2026_TIME_TRAVEL_DEMO
	AT (TIMESTAMP => DATEADD('day', -4, CURRENT_TIMESTAMP())::TIMESTAMP_TZ)
	IGNORE TABLES WITH INSUFFICIENT DATA RETENTION;


-- Optional cleanup: run this statement after the presentation.
DROP DATABASE DOD2026_TIME_TRAVEL_DEMO;
DROP DATABASE IF EXISTS RESTORED_DB;