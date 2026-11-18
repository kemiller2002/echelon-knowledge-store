#r "/home/user/praxis-learning/src/Praxis.Domain/bin/Release/net10.0/Praxis.Domain.dll"
open System
open Praxis.Domain.Learning
open Praxis.Domain.Provenance
open Praxis.Domain.Autonomy
let now = DateTimeOffset.Parse "2026-11-18T12:00:00Z"
let actor kind id = { Kind = kind; Id = id; Provider = None; Model = None; Runtime = None }
let agentA = actor ActorKind.Agent "claude-run-1"
let p target =
    { Id = "IMP-1"; Version = 1
      Failures = [ { Source = FailureSource.FailedVerification; Summary = "D-T4"; EvidenceRefs = [ "raw/round2/runs/D-T4/REPORT.json" ]; ObservedAt = now } ]
      RootCause = RootCause.ProcessGap; CorrectiveAction = "enumerate callers"; Target = target
      ExpectedImprovement = "fewer root-cause misses"; EstimatedCost = 1m; RollbackPlan = "revert"
      ProposedBy = agentA; DerivedFrom = [] }
let case id ok = { CaseId = id; RootCause = RootCause.ProcessGap; Passed = ok; EvidenceRef = "" }
let show label r = printfn "%-62s -> %A" label r
// 1. held-out = same case under a different id string (case/whitespace variant): accepted, no evidence ref
let ev1 = { Counterfactual = [ case "D-T4" true ]; HeldOut = [ case "d-t4 " true ]; Regression = [ case "D-T1" true ]; EvaluatedAt = now }
let prop = p (ImprovementTarget.Prompt "arm-D")
let s1 = ImprovementProposal.evaluate prop ProposalState.Proposed ev1
show "held-out 'd-t4 ' vs counterfactual 'D-T4', blank EvidenceRef" s1
// 2. another agent (or Unknown kind) approves a prompt/skill/knowledge change: no human needed
match s1 with
| Ok st ->
    show "Prompt change approved by another agent" (ImprovementProposal.approve prop st (actor ActorKind.Agent "claude-run-2") now |> Result.map (fun _ -> "Approved"))
    show "Prompt change approved by Unknown-kind actor" (ImprovementProposal.approve prop st (actor ActorKind.Unknown "x") now |> Result.map (fun _ -> "Approved"))
    show "self-approval with same agent, id casing changed" (ImprovementProposal.approve prop st (actor ActorKind.Agent "Claude-Run-1") now |> Result.map (fun _ -> "Approved"))
    // 3. verification guidance relabelled as Knowledge escapes the human gate
    let kp = p (ImprovementTarget.Knowledge "AGENTS.md")
    show "Knowledge AGENTS.md (verification rules) by another agent" (ImprovementProposal.evaluate kp ProposalState.Proposed ev1 |> Result.bind (fun st -> ImprovementProposal.approve kp st (actor ActorKind.Agent "claude-run-2") now) |> Result.map (fun _ -> "Approved"))
| Error e -> printfn "%A" e
// 4. state is not bound to the proposal: evaluate P, approve a *different* proposal record (proposer swapped) with the Evaluated state
let gov = p (ImprovementTarget.Governance ProtectedClass.VerificationPolicy)
let humanSelf = actor ActorKind.Human "kevin"
let govByKevin = { gov with ProposedBy = humanSelf }
match ImprovementProposal.evaluate govByKevin ProposalState.Proposed ev1 with
| Ok st ->
    show "governance: proposer kevin approves own (should refuse)" (ImprovementProposal.approve govByKevin st humanSelf now |> Result.map (fun _ -> "Approved"))
    show "governance: same Evaluated state, proposal record says agent" (ImprovementProposal.approve { govByKevin with ProposedBy = agentA; Target = ImprovementTarget.Governance ProtectedClass.SecurityPolicy } st humanSelf now |> Result.map (fun _ -> "Approved"))
    // 5. human kind is self-declared
    show "governance by agent, approver self-declared Human 'ops'" (ImprovementProposal.approve gov st (actor ActorKind.Human "ops") now |> Result.map (fun _ -> "Approved"))
| Error e -> printfn "%A" e
