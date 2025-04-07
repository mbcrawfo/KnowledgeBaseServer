-- Renames memory_node columns to use 'outdated' instead of 'removed'.

alter table memory_nodes rename column removed to outdated;
alter table memory_nodes rename column removal_reason to outdated_reason;
