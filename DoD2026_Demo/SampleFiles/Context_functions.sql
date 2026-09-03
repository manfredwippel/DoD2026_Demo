-- Context functions provide information about the statement execution context.

-- Shortcuts: Command+Enter (Ctrl+Enter on Windows); Command+/ to comment out.
SELECT
	CURRENT_ROLE(),
	CURRENT_USER();

SELECT
	CURRENT_DATABASE(),
	CURRENT_SCHEMA(),
	CURRENT_SESSION(),
	CURRENT_WAREHOUSE(),
	CURRENT_STATEMENT();

-- Some context functions can be called with or without parentheses.
SELECT
	CURRENT_DATE,
	CURRENT_DATE();

SELECT
	CURRENT_DATE,
	CURRENT_TIME,
	CURRENT_TIMESTAMP,
	CURRENT_USER,
	LOCALTIME,
	LOCALTIMESTAMP;

-- SYSDATE uses the UTC time zone.
SELECT
	SYSDATE(),
	CURRENT_TIMESTAMP();

-- Display the organization, account, and warehouse names.
SELECT
	CURRENT_ORGANIZATION_NAME(),
	CURRENT_ACCOUNT(),
	CURRENT_WAREHOUSE();

-- Switch to ORGADMIN to display organization accounts.
USE ROLE ORGADMIN;

SHOW ORGANIZATION ACCOUNTS;