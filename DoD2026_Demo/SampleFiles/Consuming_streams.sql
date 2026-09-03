/*
Change source_table and use a stream to capture the changes that update target_table.
*/

USE SCHEMA DOD2026_DB.DOD2026_DEMO;
USE WAREHOUSE DOD_WAREHOUSE;

-- Create the source table.
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

SELECT *
FROM source_table;

-- Create a stream object.
CREATE OR REPLACE STREAM sample_stream
	ON TABLE source_table;

SHOW STREAMS;

-- Create a stream on a view.
CREATE OR REPLACE SECURE VIEW source_view AS
SELECT *
FROM orders AS o
INNER JOIN customer AS c
	ON o.o_custkey = c.c_custkey;

CREATE OR REPLACE STREAM sample_stream
	ON VIEW source_view;

-- Display the stream's base tables.
SHOW STREAMS;

DROP VIEW source_view;

-- Recreate the stream on the source table.
CREATE OR REPLACE STREAM sample_stream
	ON TABLE source_table;

/*
Stream types:
- Standard (delta) streams track inserts, deletes, and updates.
- Append-only streams track inserts only.
*/

-- Validate the stream.
SELECT *
FROM sample_stream;

-- Create the target table.
CREATE OR REPLACE TABLE target_table AS
SELECT *
FROM source_table;

SELECT *
FROM target_table;

/*
METADATA$ACTION    METADATA$ISUPDATE

INSERT             FALSE                Insert
DELETE             FALSE                Delete
INSERT             TRUE                 Update (new row)
DELETE             TRUE                 Update (old row)
*/

/* 1. INSERT */

INSERT INTO source_table
VALUES (4, 200, CURRENT_TIME(), 'task_1');

-- Show how the stream captures an inserted row.
SELECT *
FROM sample_stream;

-- Consume inserts from the stream.
MERGE INTO target_table AS trg
USING (
	SELECT *
	FROM sample_stream
) AS mrg
ON trg.id = mrg.id
WHEN NOT MATCHED
	AND mrg.METADATA$ACTION = 'INSERT'
	AND mrg.METADATA$ISUPDATE = FALSE
	THEN INSERT (id, price, insert_time, insert_task)
		VALUES (mrg.id, mrg.price, mrg.insert_time, mrg.insert_task);

-- Validate the target table and consumed stream.
SELECT *
FROM target_table;

SELECT *
FROM sample_stream;

/* 2. DELETE */

SELECT *
FROM source_table;

DELETE FROM source_table
WHERE id = 4;

-- Validate the delete.
SELECT *
FROM source_table;

SELECT *
FROM sample_stream;

SELECT *
FROM target_table;

-- Check whether the stream contains data.
SELECT SYSTEM$STREAM_HAS_DATA('sample_stream');

-- Consume deletes from the stream.
MERGE INTO target_table AS trg
USING (
	SELECT *
	FROM sample_stream
) AS mrg
ON trg.id = mrg.id
WHEN MATCHED
	AND mrg.METADATA$ACTION = 'DELETE'
	AND mrg.METADATA$ISUPDATE = FALSE
	THEN DELETE;

-- Validate the source, stream, and target.
SELECT *
FROM source_table;

SELECT *
FROM sample_stream;

SELECT *
FROM target_table;

SELECT SYSTEM$STREAM_HAS_DATA('sample_stream');

/* 3. UPDATE */

SELECT *
FROM source_table;

UPDATE source_table
SET price = 1000
WHERE id = 1;

-- Validate the update.
SELECT *
FROM source_table;

SELECT *
FROM sample_stream;

SELECT *
FROM target_table;

-- Consume updates from the stream.
MERGE INTO target_table AS trg
USING (
	SELECT *
	FROM sample_stream
) AS mrg
ON trg.id = mrg.id
WHEN MATCHED
	AND mrg.METADATA$ACTION = 'INSERT'
	AND mrg.METADATA$ISUPDATE = TRUE
	THEN UPDATE SET
		trg.id = mrg.id,
		trg.price = mrg.price,
		trg.insert_time = mrg.insert_time,
		trg.insert_task = mrg.insert_task;

-- Validate the source, stream, and target.
SELECT *
FROM source_table;

SELECT *
FROM sample_stream;

SELECT *
FROM target_table;

/* Test multiple DML statements. */

-- Create empty source and target tables.
CREATE OR REPLACE TABLE DOD2026_DB.DOD2026_DEMO.source_table (
	id INT,
	price NUMBER,
	insert_time TIME,
	insert_task VARCHAR
);

CREATE OR REPLACE TABLE target_table AS
SELECT *
FROM source_table;

-- Recreate the stream because replacing its source table makes it stale.
CREATE OR REPLACE STREAM sample_stream
	ON TABLE source_table;

-- Validate the empty objects.
SELECT *
FROM source_table;

SELECT *
FROM sample_stream;

SELECT *
FROM target_table;

-- Insert three rows.
INSERT INTO source_table
VALUES
	(1, 50, CURRENT_TIME(), 'task_1'),
	(2, 400, CURRENT_TIME(), 'task_1'),
	(3, 500, CURRENT_TIME(), 'task_1');

SELECT *
FROM sample_stream;

-- Update two rows.
UPDATE source_table
SET
	insert_time = CURRENT_TIME(),
	price = 900
WHERE id = 3;

UPDATE source_table
SET id = 5
WHERE id = 2;

SELECT *
FROM sample_stream;

-- Delete one row.
DELETE FROM source_table
WHERE id = 1;

-- The stream contains three inserts, two updates, and one delete.
SELECT *
FROM sample_stream;

-- Consume all DML statement types from the stream.
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

-- Validate the final state.
SELECT *
FROM source_table;

SELECT *
FROM sample_stream;

SELECT *
FROM target_table;

/* Cleanup */

DROP STREAM sample_stream;
DROP TABLE source_table;
DROP TABLE target_table;