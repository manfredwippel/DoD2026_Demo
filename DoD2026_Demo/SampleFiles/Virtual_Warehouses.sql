/*
Demonstrate creating virtual warehouses, configuring warehouse parameters,
and observing warehouse behavior.

Virtual warehouses can be created through the web interface or SQL.
*/

SHOW WAREHOUSES;

-- Create a warehouse with the most basic syntax.
CREATE OR REPLACE WAREHOUSE test_warehouse_1;

SHOW WAREHOUSES;

ALTER WAREHOUSE test_warehouse_1 SUSPEND;
ALTER WAREHOUSE test_warehouse_1 RESUME;

-- Create a warehouse with common parameters.
CREATE OR REPLACE WAREHOUSE test_warehouse_1
	WAREHOUSE_SIZE = XSMALL
	AUTO_SUSPEND = 300
	INITIALLY_SUSPENDED = TRUE;

SHOW WAREHOUSES;

-- Create a multi-cluster warehouse.
CREATE OR REPLACE WAREHOUSE test_warehouse_1
	WAREHOUSE_SIZE = XSMALL
	AUTO_SUSPEND = 120
	INITIALLY_SUSPENDED = TRUE
	ENABLE_QUERY_ACCELERATION = FALSE
	MAX_CLUSTER_COUNT = 10
	MIN_CLUSTER_COUNT = 1
	SCALING_POLICY = ECONOMY;

SHOW WAREHOUSES;

DROP WAREHOUSE test_warehouse_1;

-- Use the DoD 2026 demo warehouse.
USE WAREHOUSE DOD_WAREHOUSE;

SHOW WAREHOUSES;

-- Run DDL statements and observe warehouse state.
CREATE OR REPLACE DATABASE test_db;
CREATE OR REPLACE SCHEMA test_schema;

CREATE TABLE test_table (
	col1 INT,
	col2 VARCHAR,
	col3 DATE
);

SHOW WAREHOUSES;

INSERT INTO test_table
VALUES
	(1, 'Snowflake', '2024-06-01'),
	(2, 'Snowflake 2', '2024-06-01'),
	(3, 'Snowflake 3', '2024-06-01'),
	(4, 'Snowflake 4', '2024-06-01');

SHOW WAREHOUSES;

SELECT COUNT(*)
FROM test_table;

-- Suspend the active warehouse.
ALTER WAREHOUSE DOD_WAREHOUSE SUSPEND;

-- The query automatically resumes a warehouse configured with AUTO_RESUME.
SELECT COUNT(*)
FROM test_table;

SHOW WAREHOUSES;

/* Cleanup */

-- The first statement intentionally demonstrates an error for a missing object.
DROP WAREHOUSE test_warehouse_1;
DROP WAREHOUSE IF EXISTS test_warehouse_1;
DROP DATABASE test_db;