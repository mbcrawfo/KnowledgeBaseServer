-- Adds an importance column to the memory_nodes table and sets all existing rows to importance 50.

alter table memory_nodes
    add column importance integer default 50 not null check (importance between 0 and 100);

create index idx_memory_nodes_importance_desc on memory_nodes (importance desc);
