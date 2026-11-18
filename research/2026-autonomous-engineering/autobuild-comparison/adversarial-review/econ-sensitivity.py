import json, sys
s = json.load(open(sys.argv[1]))["round2"]["arms"]
Q, R, k = 5, 60, 0.1
def per(arm, fc_adj=0, inc=0, q_adj=0, Rinc=None):
    a = s[arm]; Rinc = R if Rinc is None else Rinc
    total = a["meanWallSeconds"]*a["n"]/60*k + (a["requesterQuestions"]+q_adj)*Q + (a["falseCompletionClaims"]+fc_adj)*R + inc*Rinc
    return round(total/a["verified"], 2)
print("central, as published:      ", {a: per(a) for a in "ABCDEN"})
print("D-T4 disclosed (no R):      ", {**{a: per(a) for a in "ABCEN"}, "D": per("D", fc_adj=-1)})
print("+ incompletes need redo (R):", {**{a: per(a) for a in "ABCE"}, "D": per("D", fc_adj=-1, inc=1, q_adj=-1), "N": per("N", inc=1, q_adj=-1)})
# break-even R between A and B
a, b = s["A"], s["B"]
fixedA = a["meanWallSeconds"]*6/60*k + a["requesterQuestions"]*Q
fixedB = b["meanWallSeconds"]*6/60*k + b["requesterQuestions"]*Q
# (fixedA + R)/5 = fixedB/6
print("break-even R (A vs B), central Q,k:", round(fixedB/6*5 - fixedA, 1), "min")
for q in (2, 5, 10):
    fa = a["meanWallSeconds"]*6/60*k + a["requesterQuestions"]*q; fb = b["meanWallSeconds"]*6/60*k + b["requesterQuestions"]*q
    print(f"  Q={q}: A cheaper than B iff R < {round(fb/6*5 - fa,1)} min")
