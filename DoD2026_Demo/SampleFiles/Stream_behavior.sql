/*
Demonstrate stream consumption behavior, consuming a stream in multiple DML
statements, stream staleness, and append-only streams.
*/

USE SCHEMA DOD2026_DB.DOD2026_DEMO;
USE WAREHOUSE DOD_WAREHOUSE;

-- Create the source table, target table, and source-table stream.
CREATE OR REPLACE TABLE DOD2026_DB.DOD2026_DEMO.source_table (
	id INT,
	price NUMBER,
	insert_time TIME,
	insert_task VARCHAR
);

CREATE OR REPLACE TABLE target_table AS
SELECT *
FROM source_table;

CREATE OR REPLACE STREAM sample_stream
	ON TABLE source_table;

/* 1. Stream consumption */

-- A DML statement consumes the stream even when it changes no target rows.
SELECT *
FROM sample_stream;

INSERT INTO source_table
VALUES (6, 600, CURRENT_TIME(), 'task_1');

SELECT *
FROM sample_stream;

-- This intentionally fails because streams cannot be truncated.
TRUNCATE STREAM sample_stream;

-- This DML statement changes no rows but still consumes the stream.
INSERT INTO target_table (id, price, insert_time, insert_task)
SELECT
	id,
	price,
	insert_time,
	insert_task
FROM sample_stream
WHERE 1 = 2;

INSERT INTO target_table (id)
SELECT id
FROM sample_stream
WHERE 1 = 2;

-- Validate the consumed stream and unchanged tables.
SELECT *
FROM sample_stream;

SELECT *
FROM source_table;

SELECT *
FROM target_table;

-- Use a transaction to consume the same stream data in multiple DML statements.
INSERT INTO source_table
VALUES (7, 100, CURRENT_TIME(), 'task_1');

SELECT *
FROM sample_stream;

SELECT *
FROM source_table;

SELECT *
FROM target_table;

BEGIN;

-- This DML statement changes no rows.
INSERT INTO target_table (id, price, insert_time, insert_task)
SELECT
	id,
	price,
	insert_time,
	insert_task
FROM sample_stream
WHERE 1 = 2;

-- Insert the stream row once.
INSERT INTO target_table (id, price, insert_time, insert_task)
SELECT
	id,
	price,
	insert_time,
	insert_task
FROM sample_stream;

-- Insert the same stream row a second time in the transaction.
INSERT INTO target_table (id, price, insert_time, insert_task)
SELECT
	id,
	price,
	insert_time,
	insert_task
FROM sample_stream;

COMMIT;

-- Validate the transaction results.
SELECT *
FROM sample_stream;

SELECT *
FROM source_table;

SELECT *
FROM target_table;

/* 2. Staleness */

SHOW STREAMS;

-- STALE_AFTER depends on the source object's retention period.
-- Snowflake extends the period to 14 days regardless of account edition.
SHOW TABLES LIKE 'source_table';

SHOW TABLES;

-- Set the source table retention period to one day.
ALTER TABLE source_table
	SET DATA_RETENTION_TIME_IN_DAYS = 1;

SHOW STREAMS;

-- Consuming a stream also updates STALE_AFTER.
SHOW STREAMS;

INSERT INTO source_table
VALUES (6, 600, CURRENT_TIME(), 'task_1');

-- Consume the stream without changing target rows.
INSERT INTO target_table (id, price, insert_time, insert_task)
SELECT
	id,
	price,
	insert_time,
	insert_task
FROM sample_stream
WHERE 1 = 2;

SHOW STREAMS;

/* 3. Append-only streams */

CREATE OR REPLACE STREAM sample_stream
	ON TABLE source_table
	APPEND_ONLY = TRUE;

SELECT *
FROM sample_stream;

-- Inserts are captured.
INSERT INTO source_table
VALUES (7, 700, CURRENT_TIME(), 'task_1');

SELECT *
FROM sample_stream;

-- Updates are not captured.
UPDATE source_table
SET price = 1700
WHERE id = 7;

SELECT *
FROM sample_stream;

-- Deletes are not captured.
DELETE FROM source_table
WHERE id = 7;

SELECT *
FROM sample_stream;

/* Cleanup */

DROP STREAM sample_stream;
DROP TABLE source_table;
DROP TABLE target_table;