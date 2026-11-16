# Epistemic state results

## Corpus measurements (`results/ablation.md`, `results/validation.md`)

24 hypotheses in the public corpus, declared status against the standing derived
from evidence links:

| Declared status | Evidence standing | Records |
|---|---|---|
| supported | supported | 8 |
| rejected | refuted | 1 |
| active / proposed | unassessed | 10 |
| active / proposed | supported | 4 |
| active | refuted | 1 |
| proposed | contested | 1 |

* No hypothesis claims "supported" without supporting evidence (0
  `unsupported-claim` violations). The Praxis research convention works.
* 5 hypotheses have evidence that has moved on while their status has not
  (4 supported and 1 refuted while still "active/proposed"). Status lags evidence.
* 3 support links come from records that are themselves superseded.
* Confidence is qualitative (very-low … high) everywhere; no record uses a number.
  Nothing in the corpus calibrates a number, so EKS keeps qualitative confidence.
* Evidence carries `retrieved`/`source_date` in the research template but not in
  ROS evidence (49 records cite sources only through links).

## Agent trials

Epistemic questions (Q05, Q15: status, confidence, contested?) were answered
exactly in every metadata-bearing condition. With metadata stripped they scored
0, 0.67, 0.67 and 0.67: agents guessed status ("proposed", "refuted") and
confidence from prose, and one inferred support that no record states.

## Conclusions

* Explicit epistemic fields (status, confidence, supporting and contradicting
  evidence) are what let agents separate claims from evidence. Prose alone
  produces confident guesses.
* Standing should be **derived** from evidence links, and a validator should flag
  status/standing disagreement rather than store standing by hand.
* Not tested: adversarial cases (misleading authority, unverified AI-generated
  claims, injected instructions). Specified in `architecture/epistemic-model.md`
  and `architecture/security-model.md`.
