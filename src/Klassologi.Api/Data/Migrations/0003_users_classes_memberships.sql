-- Användare, klasser och medlemskap med roll per klass. Beslut: Q3, Q5 och Q12 i Specifications/Questions.md.
-- Id:n är UUID v7 som text. Tekniska värden använder ascii_bin, text för människor databasens svenska collation.

CREATE TABLE users (
    Id              CHAR(36)     CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    DisplayName     VARCHAR(100) NOT NULL,
    IsAdministrator BOOLEAN      NOT NULL DEFAULT FALSE,
    CreatedAt       DATETIME(6)  NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
    PRIMARY KEY (Id)
);

CREATE TABLE classes (
    Id        CHAR(36)     CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    Name      VARCHAR(100) NOT NULL,
    CreatedBy CHAR(36)     CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    CreatedAt DATETIME(6)  NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
    PRIMARY KEY (Id),
    CONSTRAINT fk_classes_created_by FOREIGN KEY (CreatedBy) REFERENCES users (Id)
);

CREATE TABLE class_memberships (
    ClassId   CHAR(36)    CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    UserId    CHAR(36)    CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    Role      VARCHAR(32) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    CreatedAt DATETIME(6) NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
    UpdatedAt DATETIME(6) NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
    PRIMARY KEY (ClassId, UserId),
    KEY ix_class_memberships_user (UserId),
    CONSTRAINT fk_class_memberships_class FOREIGN KEY (ClassId) REFERENCES classes (Id) ON DELETE CASCADE,
    CONSTRAINT fk_class_memberships_user FOREIGN KEY (UserId) REFERENCES users (Id),
    CONSTRAINT ck_class_memberships_role CHECK (Role IN ('Student', 'Teacher', 'StudentRepresentative'))
);
