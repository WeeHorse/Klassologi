-- Inloggningsuppgifter för ASP.NET Core Identity (Q2). E-postadressen är inloggningsnamn.
-- NormalizedEmail jämförs exakt (utf8mb4_bin) eftersom Identity redan har normaliserat värdet.

ALTER TABLE users
    ADD COLUMN Email             VARCHAR(256) NULL,
    ADD COLUMN NormalizedEmail   VARCHAR(256) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NULL,
    ADD COLUMN PasswordHash      VARCHAR(512) CHARACTER SET ascii COLLATE ascii_bin NULL,
    ADD COLUMN SecurityStamp     VARCHAR(64)  CHARACTER SET ascii COLLATE ascii_bin NULL,
    ADD COLUMN LockoutEnd        DATETIME(6)  NULL,
    ADD COLUMN AccessFailedCount INT          NOT NULL DEFAULT 0,
    ADD UNIQUE KEY ux_users_normalized_email (NormalizedEmail);
