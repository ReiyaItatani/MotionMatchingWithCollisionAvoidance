# Collision Avoidance System

This repository contains a Collision Avoidance system for agents in a simulated environment, integrating various behavioral forces to navigate towards goals while avoiding collisions and forming coherent groups.

## System Overview

Agents in the system are influenced by a combination of forces:
- **Goal Direction**: Propels the agent towards a designated target.
- **Collision Avoidance Logic**: Prevents collisions with nearby agents using Nuria HiDAC's logic.
- **Unaligned Collision Avoidance**: Predicts and mitigates potential collisions, as per Reynolds (1987).
- **Force from Group**: Encourages group behaviors, detailed by Moussaid et al. (2010).
- **Wall Force**: Generates a repulsive force from walls to avoid collisions, following Nuria HiDAC's principles.

Weights assigned to each force can be adjusted to modify their impact on agent movement.

## Key Features

### Motion Matching

For realistic animation of local movements, motion matching techniques are employed. Details can be found at [MotionMatching GitHub](https://github.com/JLPM22/MotionMatching).

> **Note**: Movements of the head, neck, and eyes are specifically designed to account for collision scenarios.

### Collision Avoidance Logic

- **Field of View (FOV)**: Adjusts in size based on the agent's upper body animation, affecting the Collision Avoidance Logic.
- **Avoidance Direction**: Ensures agents avoid moving in the same direction when evading each other.
- **Group Dynamics**: The avoidance force scales with the size of the group, with a formula of `radius + 1f` for the avoidance vector.
- **Distance-Based Scaling**: The force varies depending on the proximity of other agents.

### Force from Group

This force is a composite of:
- **Cohesion**: Draws agents towards the group center.
- **Repulsion**: Repels agents when they get too close to each other.
- **Alignment**: Aligns agents to move in the same direction when in a group.

Future updates will include social relation-based distance adjustments.

### Field of View Adjustments

The FOV is dynamic and correlates with the agent's current upper body animation:
- **Using Smartphone**: Narrow FOV at 30 degrees.
- **Talking**: Moderate FOV at 60 degrees.
- **Walking**: Wide FOV at 120 degrees.

### Upper Body Animation

Determined by the agent's social relations, which include Couple, Friend, Family, Coworker, and Individual. Agents interact or transition between animations based on these relations and their current state, such as walking or using a smartphone.

### Head, Eye, and Neck Movements

These movements are not animation-based but are controlled by attraction points:
- **CollidedTarget**: The agent that has been collided with.
- **CurrentAvoidanceTarget**: A potential collision target.
- **MyDirection**: The agent's current travel direction.
- **CenterOfMass**: The center of mass if the agent is part of a group.

Agents will randomly switch focus between MyDirection and CurrentAvoidanceTarget, or CenterOfMass, depending on their group status.

---

*Images and diagrams will be added to this document to visually demonstrate the system's features and behaviors.*

For contributions or further queries, please feel free to open an issue or submit a pull request.
