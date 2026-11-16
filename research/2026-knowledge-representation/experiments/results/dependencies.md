Repository-level dependency detection against the independently verified audit edges (pairs from → to).

| Gold | Scenario (target) | Method | gold pairs | predicted | TP | precision | recall |
| --- | --- | --- | --- | --- | --- | --- | --- |
| all verified edges | all | M1 typed front-matter graph | 47 | 0 | 0 | 0.00 | 0.00 |
| all verified edges | all | M2 prose keyword search | 47 | 85 | 46 | 0.54 | 0.98 |
| all verified edges | all | M3 manifests (packages, workflows, Praxis install) | 47 | 26 | 20 | 0.77 | 0.43 |
| all verified edges | all | M4 typed graph ∪ manifests | 47 | 26 | 20 | 0.77 | 0.43 |
| all verified edges | all | M5 keyword ∪ manifests | 47 | 85 | 46 | 0.54 | 0.98 |
| all verified edges | fides | M1 typed front-matter graph | 4 | 0 | 0 | 0.00 | 0.00 |
| all verified edges | fides | M2 prose keyword search | 4 | 3 | 3 | 1.00 | 0.75 |
| all verified edges | fides | M3 manifests (packages, workflows, Praxis install) | 4 | 1 | 1 | 1.00 | 0.25 |
| all verified edges | fides | M4 typed graph ∪ manifests | 4 | 1 | 1 | 1.00 | 0.25 |
| all verified edges | fides | M5 keyword ∪ manifests | 4 | 3 | 3 | 1.00 | 0.75 |
| all verified edges | forma | M1 typed front-matter graph | 10 | 0 | 0 | 0.00 | 0.00 |
| all verified edges | forma | M2 prose keyword search | 10 | 10 | 10 | 1.00 | 1.00 |
| all verified edges | forma | M3 manifests (packages, workflows, Praxis install) | 10 | 5 | 5 | 1.00 | 0.50 |
| all verified edges | forma | M4 typed graph ∪ manifests | 10 | 5 | 5 | 1.00 | 0.50 |
| all verified edges | forma | M5 keyword ∪ manifests | 10 | 10 | 10 | 1.00 | 1.00 |
| all verified edges | clarity | M1 typed front-matter graph | 3 | 0 | 0 | 0.00 | 0.00 |
| all verified edges | clarity | M2 prose keyword search | 3 | 11 | 3 | 0.27 | 1.00 |
| all verified edges | clarity | M3 manifests (packages, workflows, Praxis install) | 3 | 0 | 0 | 0.00 | 0.00 |
| all verified edges | clarity | M4 typed graph ∪ manifests | 3 | 0 | 0 | 0.00 | 0.00 |
| all verified edges | clarity | M5 keyword ∪ manifests | 3 | 11 | 3 | 0.27 | 1.00 |
| all verified edges | edf | M1 typed front-matter graph | 6 | 0 | 0 | 0.00 | 0.00 |
| all verified edges | edf | M2 prose keyword search | 6 | 11 | 6 | 0.55 | 1.00 |
| all verified edges | edf | M3 manifests (packages, workflows, Praxis install) | 6 | 0 | 0 | 0.00 | 0.00 |
| all verified edges | edf | M4 typed graph ∪ manifests | 6 | 0 | 0 | 0.00 | 0.00 |
| all verified edges | edf | M5 keyword ∪ manifests | 6 | 11 | 6 | 0.55 | 1.00 |
| all verified edges | dokimos | M1 typed front-matter graph | 9 | 0 | 0 | 0.00 | 0.00 |
| all verified edges | dokimos | M2 prose keyword search | 9 | 11 | 9 | 0.82 | 1.00 |
| all verified edges | dokimos | M3 manifests (packages, workflows, Praxis install) | 9 | 3 | 3 | 1.00 | 0.33 |
| all verified edges | dokimos | M4 typed graph ∪ manifests | 9 | 3 | 3 | 1.00 | 0.33 |
| all verified edges | dokimos | M5 keyword ∪ manifests | 9 | 11 | 9 | 0.82 | 1.00 |
| hard dependencies only | all | M1 typed front-matter graph | 29 | 0 | 0 | 0.00 | 0.00 |
| hard dependencies only | all | M2 prose keyword search | 29 | 85 | 29 | 0.34 | 1.00 |
| hard dependencies only | all | M3 manifests (packages, workflows, Praxis install) | 29 | 26 | 20 | 0.77 | 0.69 |
| hard dependencies only | all | M4 typed graph ∪ manifests | 29 | 26 | 20 | 0.77 | 0.69 |
| hard dependencies only | all | M5 keyword ∪ manifests | 29 | 85 | 29 | 0.34 | 1.00 |
| hard dependencies only | fides | M1 typed front-matter graph | 2 | 0 | 0 | 0.00 | 0.00 |
| hard dependencies only | fides | M2 prose keyword search | 2 | 3 | 2 | 0.67 | 1.00 |
| hard dependencies only | fides | M3 manifests (packages, workflows, Praxis install) | 2 | 1 | 1 | 1.00 | 0.50 |
| hard dependencies only | fides | M4 typed graph ∪ manifests | 2 | 1 | 1 | 1.00 | 0.50 |
| hard dependencies only | fides | M5 keyword ∪ manifests | 2 | 3 | 2 | 0.67 | 1.00 |
| hard dependencies only | forma | M1 typed front-matter graph | 8 | 0 | 0 | 0.00 | 0.00 |
| hard dependencies only | forma | M2 prose keyword search | 8 | 10 | 8 | 0.80 | 1.00 |
| hard dependencies only | forma | M3 manifests (packages, workflows, Praxis install) | 8 | 5 | 5 | 1.00 | 0.63 |
| hard dependencies only | forma | M4 typed graph ∪ manifests | 8 | 5 | 5 | 1.00 | 0.63 |
| hard dependencies only | forma | M5 keyword ∪ manifests | 8 | 10 | 8 | 0.80 | 1.00 |
| hard dependencies only | clarity | M1 typed front-matter graph | 2 | 0 | 0 | 0.00 | 0.00 |
| hard dependencies only | clarity | M2 prose keyword search | 2 | 11 | 2 | 0.18 | 1.00 |
| hard dependencies only | clarity | M3 manifests (packages, workflows, Praxis install) | 2 | 0 | 0 | 0.00 | 0.00 |
| hard dependencies only | clarity | M4 typed graph ∪ manifests | 2 | 0 | 0 | 0.00 | 0.00 |
| hard dependencies only | clarity | M5 keyword ∪ manifests | 2 | 11 | 2 | 0.18 | 1.00 |
| hard dependencies only | edf | M1 typed front-matter graph | 0 | 0 | 0 | 0.00 | 0.00 |
| hard dependencies only | edf | M2 prose keyword search | 0 | 11 | 0 | 0.00 | 0.00 |
| hard dependencies only | edf | M3 manifests (packages, workflows, Praxis install) | 0 | 0 | 0 | 0.00 | 0.00 |
| hard dependencies only | edf | M4 typed graph ∪ manifests | 0 | 0 | 0 | 0.00 | 0.00 |
| hard dependencies only | edf | M5 keyword ∪ manifests | 0 | 11 | 0 | 0.00 | 0.00 |
| hard dependencies only | dokimos | M1 typed front-matter graph | 4 | 0 | 0 | 0.00 | 0.00 |
| hard dependencies only | dokimos | M2 prose keyword search | 4 | 11 | 4 | 0.36 | 1.00 |
| hard dependencies only | dokimos | M3 manifests (packages, workflows, Praxis install) | 4 | 3 | 3 | 1.00 | 0.75 |
| hard dependencies only | dokimos | M4 typed graph ∪ manifests | 4 | 3 | 3 | 1.00 | 0.75 |
| hard dependencies only | dokimos | M5 keyword ∪ manifests | 4 | 11 | 4 | 0.36 | 1.00 |
