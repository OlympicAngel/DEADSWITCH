# ADR-0001: Unity (C#) as the game engine

- Status: Accepted
- Date: 2026-10-03

## Context
Mobile-first, offline-first, 2D painted art (art direction since changed to stylized 3D, see ADR-0007) with a CRT/terminal HUD and a living base view. Needs mature mobile plumbing: in-app purchase, rewarded ads, local notifications, home-screen widgets, haptics, cloud save. Team is small and uses AI coding agents, so a typed language with a strong ecosystem helps.

## Decision
Use **Unity 6 LTS (6000.x)** with C#. Game rules live outside Unity in `Deadswitch.Sim` (ADR-0002); Unity is the presentation and platform layer.

## Consequences
- Good: best mobile SDK coverage; C# shared between sim, tests and engine; large asset/tooling ecosystem.
- Bad: heavier editor; YAML scene merges need care (see `.gitattributes`); some Unity cache and hub data lands on C: despite install-location settings.

## Alternatives considered
- Godot 4 (C#): lighter and free, but weaker first-party mobile monetization and notification plumbing. Re-evaluate only if Unity licensing becomes a problem; the sim core makes a switch tractable.
- React Native / Flutter: poor fit for animated base diorama and live battles.
