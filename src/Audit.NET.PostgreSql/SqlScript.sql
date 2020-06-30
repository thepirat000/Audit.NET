CREATE TABLE public.event
(
    id SERIAL NOT NULL,
    inserted_date timestamp without time zone NOT NULL DEFAULT now(),
    updated_date timestamp without time zone NOT NULL DEFAULT now(),
    data jsonb NOT NULL,
    event_type varchar(50) NULL,
    "user" varchar(50) NULL,
    CONSTRAINT event_pkey PRIMARY KEY (id)
)
WITH (
    OIDS = FALSE
)
TABLESPACE pg_default;
