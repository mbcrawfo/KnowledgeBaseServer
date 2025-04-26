-- Removes the memory_contexts table and moves the data inline to memory_nodes.

PRAGMA foreign_keys=off;

alter table memory_nodes add column context text null;

update memory_nodes
set context = (select value from memory_contexts where id = memory_nodes.context_id);

update memory_nodes
set context_id = null;

delete from memory_contexts;

drop index idx_memories_context_id;

-- Tried the trick of copying the data to a new table and renaming with FKs off.  For some reason that led to FK
-- violations with the memory_edges table when FKs are re-enabled... So we will leave the table and column in place
-- since we can't delete the FK.
alter table memory_nodes rename column context_id to _deprecated_context_id;
alter table memory_contexts rename to _deprecated_memory_contexts;

PRAGMA foreign_keys=on;
