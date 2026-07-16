\# Orbit Simulate Part For Toward Astral Harbor



This is a high-performance General Relativity (GR) orbital numerical calculation core developed for the space simulation game \*\*Toward Astral Harbor\*\*. By utilizing double-precision floating-point numbers, ADM decomposition, and symplectic integration algorithms, this project achieves precise simulation of geodesic motion under the Kerr metric within Unity.



\---



\## Physics \& Simulation Principles



The core logic of this project is extensively based on the research findings of \*\*Bacchini et al. (2018)\*\*.



\### 1. Spacetime Metric Calculation



The system dismantles the four-dimensional spacetime line element $ds^2$ via \*\*3+1 (ADM) Decomposition\*\*, and calculates the metric components in real-time within `RelativityMath.cs` (based on Boyer-Lindquist coordinates).



\### 2. Hamiltonian Evolution Equations



The core of the simulation is the transformation of the geodesic equations into a system of first-order Hamiltonian equations. The system evolves \*\*Covariant Momentum ($u\_i$)\*\* rather than ordinary velocity. The evolution logic is as follows:



\*   \*\*Position Evolution\*\*: $\\frac{dx^i}{dt} = \\frac{\\gamma^{ij} u\_j}{u^0} - \\beta^i$

\*   \*\*Momentum Evolution\*\*: $\\frac{du\_i}{dt} = -\\alpha u^0 \\partial\_i \\alpha + u\_k \\partial\_i \\beta^k - \\frac{\\gamma^{jk} u\_j u\_k}{2 u^0} \\partial\_i \\gamma^{jk} + \\text{Newtonian Perturbations}$



Where $u^0$ is the time component of the four-velocity.



\### 3. Symplectic Integration Algorithms



To ensure that orbits do not suffer from "pseudo-energy drift" caused by numerical errors over long time scales or at extremely high acceleration multipliers, this project employs a \*\*Symplectic Integrator\*\*:



\*   \*\*IMR (Implicit Midpoint Rule)\*\*: A type of symplectic integrator solved via numerical Jacobian Newton iteration. It possesses unconditional stability, accurately maintaining the system's energy conservation even in regions with extreme gravitational gradients, such as near a black hole's event horizon.



\---



\## Features \& Controls



\### 1. Spaceship Control System



The project features a built-in spaceship dynamics model following relativistic logic, supporting 2D free movement on the equatorial plane:



\*   \*\*Roll\*\*: Use `A` / `D` to rotate the ship left or right.

\*   \*\*Throttle\*\*:

&#x20;   \*   `Left Shift` / `Left Ctrl`: Incrementally Increase / Decrease engine thrust.

&#x20;   \*   `Z` / `X`: Instantly set throttle to Maximum (100%) / Off (0%).

\*   \*\*Relativistic Thrust\*\*: The change in covariant velocity produced by the engine is modulated by the local spacetime's $u^0$.



\### 2. View Control



\*   \*\*Rotate View\*\*: Hold `Right Mouse Button` and drag.

\*   \*\*Zoom View\*\*: Scroll `Middle Mouse Wheel`.



\### 3. Visualization Settings



\*   \*\*Orbit Prediction (`GROrbitPrediction`)\*\*:

&#x20;   \*   Located on the \*\*"Sun"\*\* or the central celestial body object in the scene.

&#x20;   \*   \*\*Enable Prediction\*\*: Check to enable KSP-style future orbit prediction.

&#x20;   \*   \*\*Predicted Points\*\*: Total number of sampling points for the prediction line.

&#x20;   \*   \*\*Prediction Time Step\*\*: The duration of a single prediction step.

&#x20;   \*   \*\*Steps Per Frame\*\*: Time-slicing logic; sets the number of steps calculated per frame to distribute CPU pressure and prevent stuttering.

\*   \*\*Reference Frame Switch (`GRWorldManager`)\*\*:

&#x20;   \*   Supports switching reference frames between different celestial bodies. Select different `GRPhysicsObject` as the reference frame to facilitate orbital transfers.

\*   \*\*Camera Tracking (`CameraController`)\*\*:

&#x20;   \*   Located on the `CameraBaseRator` object.

&#x20;   \*   \*\*Target\*\*: Drag the object you wish to track into this slot.



\---



\## Scenarios



The project includes four preset scenarios covering multibody simulations from strong to weak fields:



1\.  \*\*PhysicsGR1 / 2 / 3\*\*:

&#x20;   \*   Core General Relativity test scenarios. Features black holes with different spin parameters ($a$) to observe perihelion precession, photon orbits, and frame-dragging effects.

2\.  \*\*SLIMSimulation\*\*:

&#x20;   \*   A weak-field multibody gravity scenario. Simulates the transfer orbit of the Japanese \*\*SLIM lunar lander\*\*, demonstrating the system's numerical stability under far-field, multi-body gravitational perturbations.



\---



\## Development \& Integration



\### Core Modules



\*   `RelativisticPhysics`: Core logic for GR physics simulation.

\*   `RelativityMath`: Storage of metric tensor calculation formulas.

\*   `FloatingPositionSystem`: Interface for the double-precision floating origin system.

\*   `OrbitRendering`: Content related to orbit visualization.



\---

