CREATE TABLE public.event
(
    id bigserial NOT NULL,
    inserted_date timestamp without time zone NOT NULL DEFAULT now(),
    updated_date timestamp without time zone NOT NULL DEFAULT now(),
    data jsonb,
    event_type varchar(50),
	some_date timestamp,
	some_null varchar(10),
    "user" varchar(50) NULL,
    CONSTRAINT event_pkey PRIMARY KEY (id)
)
WITH (
    OIDS = FALSE
)
TABLESPACE pg_default;