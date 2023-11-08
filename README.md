# Collision Avoidance System

Welcome to the Collision Avoidance system repository, where agents dynamically navigate a simulated environment. This system is designed to simulate realistic pedestrian behavior by integrating a variety of forces that guide agents towards their goals, enable avoidance of collisions, and facilitate natural group formations.
![Collision Avoidance System](.github/media/collision_avoidance_system.png)

## System Overview

The movement of each agent is determined by a blend of the following forces:
- **Goal Direction**: Directs the agent towards a target.
- **Collision Avoidance Logic**: Utilizes Nuria HiDAC's approach to avert imminent collisions.
- **Unaligned Collision Avoidance**: Based on the work of Reynolds (1987), it anticipates and avoids potential collisions.
- **Force from Group**: Implements the principles outlined by Moussaid et al. (2010) to foster group dynamics.
- **Wall Force**: Generates a repulsive force from walls, as per Nuria HiDAC's methodology.

The influence of these forces is governed by adjustable weights, allowing for nuanced control of agent behavior.

## Key Features

### Motion Matching

To animate local movements authentically, we employ motion matching techniques. For an in-depth look, visit [MotionMatching GitHub](https://github.com/JLPM22/MotionMatching).

> **Note**: Special attention has been given to the design of head, neck, and eye movements to ensure they are responsive to collision avoidance scenarios.

### Animation Correction

We utilize the framework from "A Conversational Agent Framework with Multi-modal Personality Expression" to adjust animations based on the OCEAN personality model. However, in our program, we primarily employ the extraversion parameter to adjust postures, such as straightening or slouching.

### Facial Expressions

Facial expressions are realized through blendshapes, using the Microsoft Rocketbox avatar's blendshapes. This requires adjustments when using different avatars. Additionally, our agents feature an automatic blinking function and lip-sync capabilities powered by the Oculus package's OVRLipSync.

### Collision Avoidance Logic

- **Field of View (FOV)**: The size of the FOV adapts according to the agent's upper body animation, which is crucial for the Collision Avoidance Logic.
- **Avoidance Direction**: Agents are programmed to avoid moving in the same direction when evading each other.
- **Group Dynamics**: The avoidance force is proportional to the group size, calculated as `radius + 1f` for the avoidance vector.
- **Distance-Based Scaling**: The force is dynamically adjusted based on the distance to other agents.

### Force from Group

This composite force consists of:
- **Cohesion**: Attracts agents towards the group's center.
- **Repulsion**: Creates a separating force between agents when they are too close.
- **Alignment**: Aligns the direction of agents within a group.

Planned enhancements include modifying distances based on social relations.

### Social Relations

A central aspect of our system is the categorization of agents into five distinct **Social Relations**: Couple, Friend, Family, Coworker, and Individual. These categories significantly influence agents' interactions and their decision-making processes, affecting their animations and movements within the environment.

### Field of View Adjustments

The FOV dynamically changes with the agent's current upper body animation:
- **Using Smartphone**: A focused FOV at 30 degrees.
- **Talking**: An engaged FOV at 60 degrees.
- **Walking**: An alert FOV at 120 degrees.

### Upper Body Animation

The agent's social relations category—Couple, Friend, Family, Coworker, or Individual—determines the upper body animation. Agents will transition between conversational or walking animations when encountering agents of the same category. Solo or individual agents will switch between smartphone use and walking animations.

### Head, Eye, and Neck Movements

Controlled by attraction points such as **CollidedTarget**, **CurrentAvoidanceTarget**, **MyDirection**, and **CenterOfMass** in group scenarios, these movements are designed to be fluid and context-sensitive, enhancing the realism of interactions.

### Additional Features

- **Vector Visualization**: Utilizing the ALINE package, we provide visualization of various vectors for better understanding and debugging of agent behaviors.

---

*Visual aids illustrating the system's mechanics and agent behaviors will be incorporated to complement the textual descriptions.*

We encourage contributions and inquiries—please open an issue or submit a pull request if you wish to collaborate or have questions.
