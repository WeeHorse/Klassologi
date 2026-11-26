-- Engångslänkar för inbjudan (UC-02, UC-04, Q15) och återställning av lösenord.
-- Bara en SHA-256-hash av token lagras.

CREATE TABLE invitations (
    Id              CHAR(36)    CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    TokenHash       BINARY(32)  NOT NULL,
    ClassId         CHAR(36)    CHARACTER SET ascii COLLATE ascii_bin NULL,
    Role            VARCHAR(32) CHARACTER SET ascii COLLATE ascii_bin NULL,
    IsAdministrator BOOLEAN     NOT NULL DEFAULT FALSE,
    CreatedBy       CHAR(36)    CHARACTER SET ascii COLLATE ascii_bin NULL,
    CreatedAt       DATETIME(6) NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
    ExpiresAt       DATETIME(6) NOT NULL,
    UsedAt          DATETIME(6) NULL,
    UsedBy          CHAR(36)    CHARACTER SET ascii COLLATE ascii_bin NULL,
    PRIMARY KEY (Id),
    UNIQUE KEY ux_invitations_token_hash (TokenHash),
    CONSTRAINT fk_invitations_class FOREIGN KEY (ClassId) REFERENCES classes (Id) ON DELETE CASCADE,
    CONSTRAINT fk_invitations_created_by FOREIGN KEY (CreatedBy) REFERENCES users (Id),
    CONSTRAINT fk_invitations_used_by FOREIGN KEY (UsedBy) REFERENCES users (Id),
    -- Antingen en inbjudan till en klass med roll, eller en administratörsinbjudan utan klass.
    CONSTRAINT ck_invitations_kind CHECK (
        (ClassId IS NOT NULL AND Role IN ('Student', 'Teacher', 'StudentRepresentative') AND IsAdministrator = FALSE)
        OR (ClassId IS NULL AND Role IS NULL AND IsAdministrator = TRUE))
);

CREATE TABLE password_resets (
    Id        CHAR(36)    CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    TokenHash BINARY(32)  NOT NULL,
    UserId    CHAR(36)    CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    CreatedBy CHAR(36)    CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    CreatedAt DATETIME(6) NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
    ExpiresAt DATETIME(6) NOT NULL,
    UsedAt    DATETIME(6) NULL,
    PRIMARY KEY (Id),
    UNIQUE KEY ux_password_resets_token_hash (TokenHash),
    CONSTRAINT fk_password_resets_user FOREIGN KEY (UserId) REFERENCES users (Id),
    CONSTRAINT fk_password_resets_created_by FOREIGN KEY (CreatedBy) REFERENCES users (Id)
);
