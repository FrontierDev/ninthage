# Another MMO Project --- Copilot Instructions

## 1. High-Level Overview

This project is a large-scale, seamless, open-world MMORPG built in
Unity 6.3 using:

-   **Client--Server architecture**
-   **Headless dedicated server (no graphics)**
-   **Hybrid authoritative movement** (client-authoritative with server
    validation)
-   **PurrNet** for networking
-   **URP (Forward+)** rendering on the client
-   **Very large world (dozens of km), seamless**
-   **Day--night cycle**
-   **Additive chunk-based world streaming**

Copilot must assume this is a long-term, production-grade MMO codebase
with strict architectural boundaries.

------------------------------------------------------------------------

## 2. Assembly Definition Structure

There are four assembly definitions:

-   `Project.Client`
-   `Project.Server`
-   `Project.Shared`
-   `Project.Editor`

### Rules

-   **Shared**: Pure logic, data models, math, gameplay systems with no
    UnityEditor dependencies.
-   **Client**: Rendering, input, VFX, UI, client-only systems.
-   **Server**: Simulation, validation, authoritative systems.
-   **Editor**: Custom inspectors, tooling, importers.

Never reference `UnityEditor` outside the Editor assembly. Never
reference Client code from Server.

------------------------------------------------------------------------

## 3. Networking Model (PurrNet)

### Server

-   Headless build
-   Simulates:
    -   Movement validation
    -   Physics authority
    -   Combat resolution
    -   Resource regeneration
    -   NPC logic
-   Manages world chunk subscriptions
-   Controls interest management

### Client

-   Handles:
    -   Rendering
    -   Input
    -   Prediction
    -   Interpolation
    -   UI

### World Objects

All world objects inherit from:

-   `WorldObject : NetworkBehaviour`

Even if the class name does not contain "Networked", it is networked.

------------------------------------------------------------------------

## 4. World Architecture

### Terrain Requirements

-   Very large open world
-   Supports:
    -   Overhangs
    -   Tunnels
    -   Caves
    -   Mesh collision
    -   Jumpable obstacles
-   Server must handle gravity and terrain collision

### Streaming Model

-   World divided into spatial chunks (grid-based)
-   Terrain loaded by visibility/LOD grid
-   Objects loaded additively per chunk
-   Chunk-level interest management
-   Clients subscribe to chunks

Chunk scenes may be loaded additively.

------------------------------------------------------------------------

## 5. Rendering

-   URP (Forward+)
-   Day--night cycle required
-   Server does not load graphics
-   Rendering logic strictly client-side

------------------------------------------------------------------------

## 7. Code Standards

-   Never omit sections of code with placeholders.
-   Always provide full explicit implementations.
-   No Lua `goto` usage.
-   Editor data must save correctly even without user interaction.
-   Network data must be deterministic and synchronized.

------------------------------------------------------------------------

## 8. Performance Constraints

-   Server must scale to many concurrent entities.
-   ECS used for galaxy-scale simulation.
-   Chunk-based simulation + subscription model preferred.
-   Avoid unnecessary object instantiation.
-   Server logic must be graphics-free.

------------------------------------------------------------------------

## 9. Design Principles

-   Static world state (no dynamic scaling of zones).
-   High-risk, high-level areas are accessible but signposted.
-   Exploration rewards (e.g., stealth traversal).
-   Large pre-max-level content range.
-   Binary zone level bands allowed.

------------------------------------------------------------------------

## 10. Copilot Behaviour Expectations

When generating code:

-   Respect assembly boundaries.
-   Respect networking model.
-   Assume server authority.
-   Avoid monolithic scripts.
-   Prefer modular, extensible systems.
-   Use explicit types and clean architecture.
-   No speculative shortcuts.
-   Production-ready patterns only.
-   Avoid placeholders or incomplete implementations.
-   Avoid Reflection.
-   Avoid making additional methods where possible.

When uncertain, default to: - Deterministic logic - Server authority -
Data-driven systems - Additive scene streaming - Chunk-based interest
management

------------------------------------------------------------------------

This document defines the architectural and systemic constraints of the
MMO project. All generated code must comply with these constraints.
