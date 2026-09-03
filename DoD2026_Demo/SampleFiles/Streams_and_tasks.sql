/*
Automate stream consumption by using a stream in a task.
*/

USE ROLE ACCOUNTADMIN;
USE SCHEMA DOD2026_DB.DOD2026_DEMO;
USE WAREHOUSE DOD_WAREHOUSE;

CREATE OR REPLACE TABLE DOD2026_DB.DOD2026_DEMO.source_table (
	id INT,
	price NUMBER,
	insert_time TIME,
	insert_task VARCHAR
);

INSERT INTO source_table
VALUES
	(1, 50, CURRENT_TIME(), 'task_1'),
	(2, 400, CURRENT_TIME(), 'task_1'),
	(3, 500, CURRENT_TIME(), 'task_1');

-- Create the stream object.
CREATE OR REPLACE STREAM sample_stream
	ON TABLE source_table;

-- Create the target table.
CREATE OR REPLACE TABLE target_table AS
SELECT *
FROM source_table;

-- Validate the initial state.
SELECT *
FROM source_table;

SELECT *
FROM sample_stream;

SELECT *
FROM target_table;

SELECT SYSTEM$STREAM_HAS_DATA('sample_stream');

-- Create a task that applies inserts, updates, and deletes to the target table.
CREATE OR REPLACE TASK stream_consumer_task
	WAREHOUSE = DOD_WAREHOUSE
	SCHEDULE = '1 minute'
	WHEN SYSTEM$STREAM_HAS_DATA('sample_stream')
AS
MERGE INTO target_table AS trg
USING (
	SELECT *
	FROM sample_stream
) AS mrg
ON trg.id = mrg.id
-- Inserts
WHEN NOT MATCHED
	AND mrg.METADATA$ACTION = 'INSERT'
	AND mrg.METADATA$ISUPDATE = FALSE
	THEN INSERT (id, price, insert_time, insert_task)
		VALUES (mrg.id, mrg.price, mrg.insert_time, mrg.insert_task)
-- Updates
WHEN MATCHED
	AND mrg.METADATA$ACTION = 'INSERT'
	AND mrg.METADATA$ISUPDATE = TRUE
	THEN UPDATE SET
		trg.id = mrg.id,
		trg.price = mrg.price,
		trg.insert_time = mrg.insert_time,
		trg.insert_task = mrg.insert_task
-- Deletes
WHEN MATCHED
	AND mrg.METADATA$ACTION = 'DELETE'
	AND mrg.METADATA$ISUPDATE = FALSE
	THEN DELETE;

ALTER TASK stream_consumer_task RESUME;

-- Test inserts.
INSERT INTO source_table
VALUES
	(4, 150, CURRENT_TIME(), 'stream_consumer_task'),
	(5, 100, CURRENT_TIME(), 'stream_consumer_task'),
	(6, 250, CURRENT_TIME(), 'stream_consumer_task');

SELECT *
FROM sample_stream;

SELECT *
FROM target_table;

-- Check task history.
SELECT *
FROM TABLE(INFORMATION_SCHEMA.TASK_HISTORY(
	SCHEDULED_TIME_RANGE_START => DATEADD('hour', -1, CURRENT_TIMESTAMP()),
	RESULT_LIMIT => 10,
	TASK_NAME => 'stream_consumer_task'
))
ORDER BY scheduled_time DESC;

-- Test an update.
SELECT *
FROM target_table;

UPDATE source_table
SET
	insert_time = CURRENT_TIME(),
	price = 900
WHERE id = 6;

SELECT *
FROM sample_stream;

SELECT *
FROM target_table;

-- Test a delete.
DELETE FROM source_table
WHERE id = 5;

SELECT *
FROM sample_stream;

SELECT *
FROM source_table;

SELECT *
FROM target_table;

-- Check task history again.
SELECT *
FROM TABLE(INFORMATION_SCHEMA.TASK_HISTORY(
	SCHEDULED_TIME_RANGE_START => DATEADD('hour', -1, CURRENT_TIMESTAMP()),
	RESULT_LIMIT => 10,
	TASK_NAME => 'stream_consumer_task'
))
ORDER BY scheduled_time DESC;

/* Cleanup */

ALTER TASK stream_consumer_task SUSPEND;
DROP TASK stream_consumer_task;
DROP STREAM sample_stream;
DROP TABLE source_table;
DROP TABLE target_table;