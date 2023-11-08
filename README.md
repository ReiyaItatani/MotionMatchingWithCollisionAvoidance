# Collision Avoidance System

Welcome to the Collision Avoidance system repository, where agents dynamically navigate a simulated environment. This system is designed to simulate realistic pedestrian behavior by integrating a variety of forces that guide agents towards their goals, enable avoidance of collisions, and facilitate natural group formations.

<img src=".github/media/collision_avoidance_system.png" alt="Collision Avoidance System" width="300"/>


## System Overview

The movement of each agent within our system is influenced by a combination of dynamic forces, each contributing to the overall behavior in a unique way. These forces include:

- **Goal Direction**: Guides the agent towards a specific target or destination.
- **Collision Avoidance Logic**: Employs advanced techniques to prevent imminent collisions. Refer to the [Collision Avoidance Logic](#collision-avoidance-logic) section for an in-depth explanation.
- **Unaligned Collision Avoidance**: Inspired by Reynolds' seminal work, this force is calculated to predict and mitigate potential collisions. More information can be found in the [Unaligned Collision Avoidance](#unaligned-collision-avoidance) section.
- **Force from Group**: Encourages natural group behaviors as described by Moussaid et al. The nuances of this force are detailed in the [Force from Group](#force-from-group) section.
- **Wall Force**: Generates a deterrent force to keep agents at a safe distance from walls, following the Nuria HiDAC's established methods.

The interplay of these forces is finely tuned through adjustable weights, providing a sophisticated level of control over the agents' movements.

### Collision Avoidance Logic

- **Field of View (FOV)**: The size of the FOV adapts according to the agent's upper body animation, which is crucial for the Collision Avoidance Logic.
- **Avoidance Direction**: Agents are programmed to avoid moving in the same direction when evading each other.
- **Group Dynamics**: The avoidance force is proportional to the group size, calculated as `radius + 1f` for the avoidance vector.
- **Distance-Based Scaling**: The force is dynamically adjusted based on the distance to other agents.

### Unaligned Collision Avoidance

- **Time to Collision**: This system continuously calculates the time until a potential collision for each agent within a Box Collider's bounds.
- **Priority Targeting**: When multiple agents are present, the one with the shortest time to collision is prioritized.
- **Responsive Force**: A responsive force is applied to the agent with the highest risk of collision, guiding it away from the impending impact.

This proactive approach ensures that agents react in time to avoid collisions, maintaining smooth and uninterrupted movement throughout the environment.

### Force from Group

Agents experience a composite force that promotes cohesive group movement, consisting of:

- **Cohesion**: Draws agents towards the collective center of the group.
- **Repulsion**: Generates a dispersing force to maintain comfortable spacing between agents.
- **Alignment**: Encourages agents to move in a unified direction, mirroring the group's overall orientation.

Future updates aim to refine these interactions further, taking into account the social relationships between agents to adjust distances accordingly.

## Key Features

### Motion Matching

To animate local movements authentically, we employ motion matching techniques. For an in-depth look, visit [MotionMatching GitHub](https://github.com/JLPM22/MotionMatching).

> **Note**: Head, neck, and eye movements operate on a separate algorithm distinct from motion matching.

### Animation Correction

We utilize the framework from "A Conversational Agent Framework with Multi-modal Personality Expression" to adjust animations based on the OCEAN personality model. However, in our program, we primarily employ the extraversion parameter to adjust postures, such as straightening or slouching.

<img src=".github/media/animation_correction.png" alt="Collision Avoidance System" width="500"/>

### Facial Expressions

Facial expressions are realized through blendshapes, using the Microsoft Rocketbox avatar's blendshapes. This requires adjustments when using different avatars. Additionally, our agents feature an automatic blinking function and lip-sync capabilities powered by the Oculus package's OVRLipSync.

<img src=".github/media/facial_expression.png" alt="Collision Avoidance System" width="700"/>

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
