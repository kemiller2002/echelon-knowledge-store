Public repositories only (11); 585 records; 252 tasks.

| Task kind | Tasks |
| --- | --- |
| lookup-by-id | 59 |
| lookup-by-description | 59 |
| find-evidence | 56 |
| change-impact | 66 |
| current-authority | 12 |

| Task kind | Strategy | n | recall | precision | complete | mean tokens | mean tool calls |
| --- | --- | --- | --- | --- | --- | --- | --- |
| change-impact | A1 grep + whole files (1 hop) | 66 | 0.468 | 0.332 | 16/66 | 41728.7 | 11.5 |
| change-impact | A2 grep + whole files (chase 6 hops) | 66 | 0.826 | 0.141 | 45/66 | 485172.4 | 201.4 |
| change-impact | A3 target file only | 66 | 0.422 | 0.437 | 15/66 | 1916.7 | 1.0 |
| change-impact | A4 BM25 excerpts (k=10) | 66 | 0.242 | 0.319 | 5/66 | 1049.9 | 1.0 |
| change-impact | F1 typed graph query (cards) | 66 | 1.000 | 1.000 | 66/66 | 1677.3 | 1.0 |
| change-impact | F2 hybrid BM25 → graph (cards) | 66 | 1.000 | 1.000 | 66/66 | 1677.3 | 1.0 |
| current-authority | A1 grep + whole files (1 hop) | 12 | 0.917 | 0.163 | 11/12 | 52227.7 | 9.0 |
| current-authority | A2 grep + whole files (chase 6 hops) | 12 | 1.000 | 0.027 | 12/12 | 605835.3 | 270.1 |
| current-authority | A3 target file only | 12 | 0.750 | 0.164 | 9/12 | 1945.3 | 1.0 |
| current-authority | A4 BM25 excerpts (k=10) | 12 | 0.833 | 0.177 | 10/12 | 1874.5 | 1.0 |
| current-authority | F1 typed graph query (cards) | 12 | 1.000 | 1.000 | 12/12 | 193.2 | 1.0 |
| current-authority | F2 hybrid BM25 → graph (cards) | 12 | 1.000 | 1.000 | 12/12 | 193.2 | 1.0 |
| find-evidence | A1 grep + whole files (1 hop) | 56 | 0.783 | 0.222 | 40/56 | 44281.8 | 11.8 |
| find-evidence | A2 grep + whole files (chase 6 hops) | 56 | 0.949 | 0.059 | 50/56 | 495110.0 | 213.0 |
| find-evidence | A3 target file only | 56 | 0.663 | 0.291 | 34/56 | 1795.7 | 1.0 |
| find-evidence | A4 BM25 excerpts (k=10) | 56 | 0.293 | 0.165 | 11/56 | 974.2 | 1.0 |
| find-evidence | F1 typed graph query (cards) | 56 | 1.000 | 1.000 | 56/56 | 342.6 | 1.0 |
| find-evidence | F2 hybrid BM25 → graph (cards) | 56 | 1.000 | 1.000 | 56/56 | 342.6 | 1.0 |
| lookup-by-description | A1 grep + whole files (1 hop) | 59 | 1.000 | 0.729 | 59/59 | 7737.9 | 5.5 |
| lookup-by-description | A2 grep + whole files (chase 6 hops) | 59 | 1.000 | 0.718 | 59/59 | 94089.5 | 48.2 |
| lookup-by-description | A3 target file only | 59 | 1.000 | 0.493 | 59/59 | 1545.2 | 1.0 |
| lookup-by-description | A4 BM25 excerpts (k=10) | 59 | 0.966 | 0.097 | 57/59 | 1687.3 | 1.0 |
| lookup-by-description | F1 typed graph query (cards) | 59 | 0.000 | 0.000 | 0/59 | 0.0 | 1.0 |
| lookup-by-description | F2 hybrid BM25 → graph (cards) | 59 | 0.729 | 0.243 | 43/59 | 329.6 | 4.0 |
| lookup-by-id | A1 grep + whole files (1 hop) | 59 | 1.000 | 0.266 | 59/59 | 23787.8 | 9.2 |
| lookup-by-id | A2 grep + whole files (chase 6 hops) | 59 | 1.000 | 0.181 | 59/59 | 318469.6 | 147.6 |
| lookup-by-id | A3 target file only | 59 | 1.000 | 0.493 | 59/59 | 1545.2 | 1.0 |
| lookup-by-id | A4 BM25 excerpts (k=10) | 59 | 0.203 | 0.095 | 12/59 | 702.8 | 1.0 |
| lookup-by-id | F1 typed graph query (cards) | 59 | 1.000 | 1.000 | 59/59 | 110.9 | 1.0 |
| lookup-by-id | F2 hybrid BM25 → graph (cards) | 59 | 1.000 | 1.000 | 59/59 | 110.9 | 1.0 |
