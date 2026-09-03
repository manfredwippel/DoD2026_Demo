-- =====================================================================
-- DoD2026 Snowflake demo: Remove all demo objects.
-- =====================================================================
USE ROLE SYSADMIN;
DROP DATABASE IF EXISTS DOD2026_DB;
DROP WAREHOUSE IF EXISTS DOD_WAREHOUSE;

USE ROLE USERADMIN;
DROP USER IF EXISTS DOD2026_USER;
DROP ROLE IF EXISTS DOD_DEVELOPER;

USE ROLE SECURITYADMIN;
DROP NETWORK POLICY IF EXISTS DOD2026_NETWORK_POLICY;
