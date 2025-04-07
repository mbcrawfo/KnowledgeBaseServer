-- Renames memory_node columns to use 'outdated' instead of 'removed'.

PRAGMA foreign_keys=off;

alter table memory_nodes rename column removed to outdated;
alter table memory_nodes rename column removal_reason to outdated_reason;

PRAGMA foreign_keys=on;
