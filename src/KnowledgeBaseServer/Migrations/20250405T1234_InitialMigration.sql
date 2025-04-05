create table topics(
    id text primary key,
    created text not null,
    name text not null unique
);

create table memory_contexts(
    id text primary key,
    created text not null,
    value text not null
);

create table memories(
    id text primary key,
    created text not null,
    topic_id text not null,
    context_id text not null,
    content text not null,
    replaced_by_memory_id text,

    foreign key(topic_id) references topics(id),
    foreign key(context_id) references memory_contexts(id),
    foreign key(replaced_by_memory_id) references memories(id)
);

create index idx_memories_topic_id on memories(topic_id);
create index idx_memories_context_id on memories(context_id);
create index idx_memories_replaced_by_memory_id on memories(replaced_by_memory_id);

create table memory_links(
    id text primary key,
    created text not null,
    from_memory_id text not null,
    to_memory_id text not null,

    foreign key(from_memory_id) references memories(id),
    foreign key(to_memory_id) references memories(id)
);

create index idx_memory_links_from_memory_id on memory_links(from_memory_id);
create index idx_memory_links_to_memory_id on memory_links(to_memory_id);

create virtual table memory_search using fts5(id unindexed, content, context, tokenize=porter);
