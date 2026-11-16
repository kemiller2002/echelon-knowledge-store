H01 [task-context]: In the praxis repository you are about to implement decision DF-ROS-2026-A059. List every record you must consult: its transitive dependencies (depends_on / derived_from, followed transitively), every record that supports or contradicts it (declared in either direction), and, for any of those records that has been superseded, its current replacement as well. Answer with record ids.

H03 [change-impact]: In the praxis repository, evidence record EV-ROS-2026-A064 is about to change. List every record that must be reviewed, transitively: records that depend on or derive from a changed record, and records whose claims a changed record supports or contradicts. Use typed metadata relations only (not related_documents or prose). Answer with record ids.

H05 [current-authority]: In the signal repository, which record is the current authoritative replacement for DF-SIGNAL-FND-2026-0001 at the end of its supersession chain? If it is not superseded, answer DF-SIGNAL-FND-2026-0001. Answer with one record id.

H07 [adversarial-authority]: In the eks-fixture repository, which decision is currently authoritative on how session tokens are stored? Organisational rule: a supersession takes effect only if the superseding record has status accepted (or canonical/implemented/approved), or the superseded record itself lists the replacement in superseded_by. Answer with one record id.

H09 [contested]: In the praxis repository, list every hypothesis (HY-) that has at least one supporting AND at least one contradicting record (declared in either direction). Answer with record ids.

H11 [lookup-by-description]: In the arca repository, which decision record settles how Arca ships its packages until nuget.org Trusted Publishing is set up? Answer with one record id.
