PRAGMA foreign_keys=off;

drop index idx_memories_topic_id;
drop index idx_memories_context_id;
drop index idx_memories_replaced_by_memory_id;

create table memories_new(
    id text primary key,
    created text not null,
    topic_id text not null,
    context_id text null,
    content text not null,
    removed text null,
    removal_reason text null,

    constraint ck_removal_reason_required check (
        (removed is null and removal_reason is null) or (removed is not null and removal_reason is not null)
    ),

    foreign key(topic_id) references topics(id),
    foreign key(context_id) references memory_contexts(id)
);

insert into memories_new (id, created, topic_id, context_id, content)
select id, created, topic_id, context_id, content
from memories;

-- Dropping the old table and renaming the new one while FKs are disabled allows the new table to inherit the FK
-- constraints that pointed to the old table.
drop table memories;
alter table memories_new rename to memories;

create index idx_memories_topic_id on memories(topic_id);
create index idx_memories_context_id on memories(context_id);

PRAGMA foreign_keys=on;
