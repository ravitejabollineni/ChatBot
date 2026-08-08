FROM liquibase/liquibase:5.0.3

RUN lpm add postgresql --global

COPY changelog /liquibase/changelog
COPY migrations /liquibase/migrations
COPY liquibase.properties /liquibase/liquibase.properties

ENTRYPOINT ["liquibase"]