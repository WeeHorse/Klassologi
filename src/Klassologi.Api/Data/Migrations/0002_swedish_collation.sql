-- Svensk collation som databasens standard: å, ä och ö är egna bokstäver och sorteras efter z.
-- Beslut: Q12 i Specifications/Questions.md.
ALTER DATABASE CHARACTER SET utf8mb4 COLLATE utf8mb4_sv_0900_ai_ci;
