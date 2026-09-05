-- runs ONCE, only when the pgdata volume is first created (empty).
-- add a DB later? `docker compose down -v` to re-seed, or CREATE it by hand.
CREATE DATABASE mediview_identity;
CREATE DATABASE mediview_studies;
CREATE DATABASE mediview_imaging;
CREATE DATABASE mediview_reporting;