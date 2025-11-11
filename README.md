# Multiplayer 3D Shooter

---

## Overview

**Multiplayer 3D Shooter** is an experimental online shooter featuring dynamic gameplay, procedural arena generation, and peer-to-peer multiplayer.

Players can create or join rooms for **2–6 participants** and compete on a **procedurally generated iceberg** made of hexagonal tiles that slowly melt and collapse.  
The goal is simple: **knock opponents into the water or eliminate them** by throwing spears that deal damage on hit.  

Occasionally, **penguins** appear on the iceberg — if hit, they explode like bombs, adding chaos to the match.

---

## Core Features

- **Multiplayer (P2P)** using the **Mirror** networking framework  
- Configurable room size (2–6 players)  
- Dynamic melting iceberg arena  
- Platforms rise and fall dynamically  
- Explosive penguins as random hazards  
- In-game chat system  
- **Host migration** (automatic host reassignment on disconnect or poor connection)  
- Advanced animation system:
  - State Machines  
  - Blend Trees  
  - Animation Layers  
  - Mesh-based control  
- Ragdoll physics for realistic player death animations and movement
- Customizable gameplay modifiers (toggle penguins, moving platforms, etc.)

---

## Architecture & Tech Stack

**Engine:** Unity 6000.0.55f1  

**Main Tools & Frameworks:**
- [Mirror](https://github.com/MirrorNetworking/Mirror) — multiplayer networking  
- [DOTween](http://dotween.demigiant.com/) — animation and tweening  
- [Zenject](https://github.com/modesttree/Zenject) — dependency injection  
- Event-driven architecture  
- Coroutines  
- NavMeshAgent for penguins navigation  
- Particle System, VFX, Post-Processing, Shader Graph  

**Programming Principles:**
- Object-Oriented Programming (OOP)  
- SOLID principles  
- Design Patterns:
  - Mediator  
  - Generic Factory  
  - State Machine
  - etc.

---

## Project Status

> **Current phase:** Work in Progress 
> Core multiplayer logic, arena behavior, and animation systems are under active development.

---

### Requirements
- PC (Windows/Linux/Mac)
- Will be published on Itch.io

