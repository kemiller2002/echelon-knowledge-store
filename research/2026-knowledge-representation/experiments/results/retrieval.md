Public repositories only (11); 587 records; 253 tasks.

| Task kind | Tasks |
| --- | --- |
| lookup-by-id | 59 |
| lookup-by-description | 59 |
| find-evidence | 56 |
| change-impact | 67 |
| current-authority | 12 |

| Task kind | Strategy | n | recall | precision | complete | mean tokens | mean tool calls |
| --- | --- | --- | --- | --- | --- | --- | --- |
| change-impact | A1 grep + whole files (1 hop) | 67 | 0.476 | 0.331 | 17/67 | 41439.4 | 11.5 |
| change-impact | A2 grep + whole files (chase 6 hops) | 67 | 0.829 | 0.139 | 46/67 | 491059.6 | 204.6 |
| change-impact | A3 target file only | 67 | 0.416 | 0.428 | 15/67 | 1940.6 | 1.0 |
| change-impact | A4 BM25 excerpts (k=10) | 67 | 0.253 | 0.315 | 6/67 | 1085.6 | 1.0 |
| change-impact | F1 typed graph query (cards) | 67 | 1.000 | 1.000 | 67/67 | 1655.9 | 1.0 |
| change-impact | F2 hybrid BM25 → graph (cards) | 67 | 1.000 | 1.000 | 67/67 | 1655.9 | 1.0 |
| current-authority | A1 grep + whole files (1 hop) | 12 | 0.917 | 0.163 | 11/12 | 52227.7 | 9.0 |
| current-authority | A2 grep + whole files (chase 6 hops) | 12 | 1.000 | 0.027 | 12/12 | 605835.3 | 271.2 |
| current-authority | A3 target file only | 12 | 0.750 | 0.164 | 9/12 | 1945.3 | 1.0 |
| current-authority | A4 BM25 excerpts (k=10) | 12 | 0.833 | 0.177 | 10/12 | 1874.5 | 1.0 |
| current-authority | F1 typed graph query (cards) | 12 | 1.000 | 1.000 | 12/12 | 193.2 | 1.0 |
| current-authority | F2 hybrid BM25 → graph (cards) | 12 | 1.000 | 1.000 | 12/12 | 193.2 | 1.0 |
| find-evidence | A1 grep + whole files (1 hop) | 56 | 0.783 | 0.222 | 40/56 | 44281.8 | 11.8 |
| find-evidence | A2 grep + whole files (chase 6 hops) | 56 | 0.949 | 0.059 | 50/56 | 495110.0 | 213.8 |
| find-evidence | A3 target file only | 56 | 0.663 | 0.290 | 34/56 | 1795.7 | 1.0 |
| find-evidence | A4 BM25 excerpts (k=10) | 56 | 0.293 | 0.165 | 11/56 | 977.3 | 1.0 |
| find-evidence | F1 typed graph query (cards) | 56 | 1.000 | 1.000 | 56/56 | 342.6 | 1.0 |
| find-evidence | F2 hybrid BM25 → graph (cards) | 56 | 1.000 | 1.000 | 56/56 | 342.6 | 1.0 |
| lookup-by-description | A1 grep + whole files (1 hop) | 59 | 1.000 | 0.681 | 59/59 | 20566.6 | 9.3 |
| lookup-by-description | A2 grep + whole files (chase 6 hops) | 59 | 1.000 | 0.669 | 59/59 | 131806.5 | 67.2 |
| lookup-by-description | A3 target file only | 59 | 1.000 | 0.523 | 59/59 | 1680.9 | 1.0 |
| lookup-by-description | A4 BM25 excerpts (k=10) | 59 | 0.949 | 0.095 | 56/59 | 1678.4 | 1.0 |
| lookup-by-description | F1 typed graph query (cards) | 59 | 0.000 | 0.000 | 0/59 | 0.0 | 1.0 |
| lookup-by-description | F2 hybrid BM25 → graph (cards) | 59 | 0.678 | 0.226 | 40/59 | 324.5 | 4.0 |
| lookup-by-id | A1 grep + whole files (1 hop) | 59 | 1.000 | 0.266 | 59/59 | 25218.5 | 9.3 |
| lookup-by-id | A2 grep + whole files (chase 6 hops) | 59 | 1.000 | 0.188 | 59/59 | 327712.6 | 150.7 |
| lookup-by-id | A3 target file only | 59 | 1.000 | 0.523 | 59/59 | 1680.9 | 1.0 |
| lookup-by-id | A4 BM25 excerpts (k=10) | 59 | 0.220 | 0.103 | 13/59 | 860.5 | 1.0 |
| lookup-by-id | F1 typed graph query (cards) | 59 | 1.000 | 1.000 | 59/59 | 106.7 | 1.0 |
| lookup-by-id | F2 hybrid BM25 → graph (cards) | 59 | 1.000 | 1.000 | 59/59 | 106.7 | 1.0 |
