-- Renames tables and columns to use graph terminology.

PRAGMA foreign_keys=off;

alter table memories rename to memory_nodes;

drop index idx_memory_links_to_memory_id;

alter table memory_links rename to memory_edges;
alter table memory_edges rename column from_memory_id to source_memory_node_id;
alter table memory_edges rename column to_memory_id to target_memory_node_id;

create index idx_memory_edges_reverse_lookup on memory_edges(target_memory_node_id, source_memory_node_id);

drop table memory_search;
create virtual table memory_search using fts5(memory_node_id unindexed, memory_content, memory_context, tokenize=porter);

insert into memory_search (memory_node_id, memory_content, memory_context)
select mn.id, mn.content, mc.value
from memory_nodes mn
left outer join memory_contexts mc on mc.id = mn.context_id;

PRAGMA foreign_keys=on;
