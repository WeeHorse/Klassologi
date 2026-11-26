-- Inlägg på klassens anslagstavla (steg 4: UC-08–10). Text i Markdown.
-- Borttagna inlägg döljs (DeletedAt) och gallras permanent efter 90 dagar.

CREATE TABLE posts (
    Id        CHAR(36)     CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    ClassId   CHAR(36)     CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    AuthorId  CHAR(36)     CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    Title     VARCHAR(150) NOT NULL,
    Body      TEXT         NOT NULL,
    CreatedAt DATETIME(6)  NOT NULL DEFAULT (UTC_TIMESTAMP(6)),
    EditedAt  DATETIME(6)  NULL,
    PinnedAt  DATETIME(6)  NULL,
    PinnedBy  CHAR(36)     CHARACTER SET ascii COLLATE ascii_bin NULL,
    DeletedAt DATETIME(6)  NULL,
    DeletedBy CHAR(36)     CHARACTER SET ascii COLLATE ascii_bin NULL,
    PRIMARY KEY (Id),
    KEY ix_posts_class (ClassId, DeletedAt),
    KEY ix_posts_deleted (DeletedAt),
    CONSTRAINT fk_posts_class FOREIGN KEY (ClassId) REFERENCES classes (Id) ON DELETE CASCADE,
    CONSTRAINT fk_posts_author FOREIGN KEY (AuthorId) REFERENCES users (Id),
    CONSTRAINT fk_posts_pinned_by FOREIGN KEY (PinnedBy) REFERENCES users (Id),
    CONSTRAINT fk_posts_deleted_by FOREIGN KEY (DeletedBy) REFERENCES users (Id)
);
