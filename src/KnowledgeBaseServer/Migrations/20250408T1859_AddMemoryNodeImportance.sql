-- Adds an importance column to the memory_nodes table and sets all existing rows to importance 50.

alter table memory_nodes
    add column importance real default 0.5 not null check (importance between 0 and 1);

create index idx_memory_nodes_importance_desc on memory_nodes (importance desc);
