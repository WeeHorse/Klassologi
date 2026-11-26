-- Sätter databasens standardteckenkodning och collation uttryckligen, oberoende av serverns inställningar.
-- Se Specifications/Technology.md (Databas: MySQL – säkerhetsåtgärder) och Q12 i Questions.md.
ALTER DATABASE CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;
