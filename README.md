# teaching-stats-reports
Reporting setup project for teaching-stats.

The following script prepares the teaching-stats reporting system, in order to setup the report data from diferent sources: the main teaching-stats engine and also from a limesurvey source. In all cases, it moves the current answers into the traching-stats reporting historical tables.

## Under developement
The project is still under development and has not been completed yet. Please, refer to each release description for further information.

## Requisites
First, run the following SQL queries only if it's the first time running this script.
```
ALTER VIEW reports.answer RENAME TO answer_all;
SELECT * INTO reports.answer FROM reports.answer_all;
CREATE INDEX answer_year_idx ON reports.answer ("year");
```

