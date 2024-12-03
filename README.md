# Cloth Animation with Unity ML-Agents

This project simulates a flag with cloth physics and allows you to experiment with cloth animation using Unity. The simulation integrates Unity's physics engine and scripts to provide realistic cloth dynamics. The environment is set up in Unity, and Python may be optionally used for advanced functionality.

---

## Code repository: 
https://github.com/sawang19/ClothAnimation

## Requirements

- **Unity Editor Version**: `2022.3.48f1`  
  It is recommended to use this exact version of Unity for compatibility. You can install this version through Unity Hub.

---

## Unity Project Setup

1. **Download the Project**  
   Download and extract the project folder (e.g., `ClothAnimation.zip`). Extract the contents to your preferred location.

2. **Open the Project in Unity**  
   - Open Unity Hub.
   - Click `Add` and navigate to the extracted project folder.
   - Select the project and ensure you open it using **Unity Editor version `2022.3.48f1`**.

3. **First-Time Project Loading**  
   The project may take some time to load when opened for the first time as Unity compiles assets and dependencies. Please be patient during this step.

4. **Open the Demo Scene**  
   Navigate to the scene in the project folder: ClothAnimation\Assets\Flag With Cloth Physics\Scene
   Open the `Demo` scene to begin working with the cloth animation simulation.

---

## Scripts and Customization

All custom scripts for this project are located in:
ClothAnimation\Assets\Flag With Cloth Physics\Script

Feel free to explore and modify these scripts to customize the behavior of the cloth physics. Key scripts include:
### **CharacterMovement.cs**
- **Purpose:** Controls the movement and animations of the character.
- **Key Features:**
  - Movement is controlled with the `WASD` keys.
  - Smooth rotation of the character towards the movement direction.
  - Implements acceleration and deceleration for realistic movement.
  - Updates the Animator to reflect movement states (e.g., idle, walking).

---

### **ClothSimulation.cs**
- **Purpose:** Manages the core cloth simulation using particles and springs.
- **Key Features:**
  - Implements spring constraints between particles for structural integrity.
  - Simulates cloth behavior under gravity, wind forces, and collisions.
  - Supports interaction with sphere and capsule colliders for collision resolution.
  - Handles self-collision detection to prevent unrealistic overlaps.
  - Provides UI controls for toggling wind effects and adjusting wind strength.

---

### **MeshRefinementUtility.cs**
- **Purpose:** Subdivides a mesh to increase its resolution for better simulation precision.
- **Key Features:**
  - Adds intermediate vertices to mesh triangles.
  - Refines UV mappings to maintain texture consistency after subdivision.
  - Ensures smoother and more detailed cloth behavior.

---

### **Octree.cs**
- **Purpose:** Implements an octree for efficient spatial queries in 3D space.
- **Key Features:**
  - Subdivides space dynamically into smaller regions (nodes).
  - Optimizes self-collision detection by querying only nearby particles.
  - Balances particle density and depth of the tree for performance.

---

### **SpatialHash.cs**
- **Purpose:** Provides a spatial hashing system for efficient collision detection.
- **Key Features:**
  - Divides space into uniform grid cells for storing particle positions.
  - Enables fast querying of neighboring particles within a given radius.
  - Uses a 3D integer coordinate system for cell identification.

---

### **Spring.cs**
- **Purpose:** Simulates spring constraints between particles in the cloth.
- **Key Features:**
  - Maintains a fixed rest length between two particles connected by a spring.
  - Applies forces to ensure particles remain within the spring's constraints.
  - Includes damping to minimize oscillations and improve stability.

---

### **Particle.cs**
- **Purpose:** Represents an individual particle in the cloth simulation.
- **Key Features:**
  - Tracks position, velocity, and acceleration of the particle.
  - Uses Verlet integration for stable physics calculations.
  - Applies external forces like gravity, wind, and damping.
  - Supports pinning specific particles to act as fixed anchors.

---

### **Key Relationships**
- **ClothSimulation.cs**: Orchestrates the entire cloth simulation, using **Particle.cs** and **Spring.cs** to simulate the cloth's physical behavior.
- **MeshRefinementUtility.cs**: Prepares a high-resolution mesh for improved simulation.
- **Octree.cs** and **SpatialHash.cs**: Optimize collision detection by limiting the scope of checks to relevant particles.
- **CharacterMovement.cs**: Adds interactive character movement and animations, allowing the user to interact with the simulated cloth.

---

## **How to Use**
1. Attach `ClothSimulation.cs` to the cloth GameObject in Unity.
2. Use `CharacterMovement.cs` for controlling a player character in the scene.
3. Refine your mesh using `MeshRefinementUtility.cs` for higher precision.
4. Tweak parameters like wind strength, gravity, or stiffness in the Unity Inspector to customize the simulation.

---

## Running the Simulation

1. **Set Up the Scene**  
   Ensure the `Demo` scene is open in Unity. You should see the flag set up with physics applied.

2. **Play the Simulation**  
   Click the `Play` button in the Unity Editor to start the simulation. You can observe the cloth physics in action.

3. **Modify the Physics**  
   Use the inspector to tweak parameters like wind strength, stiffness, or gravity to see how they affect the flag's behavior.

---

## Additional Notes

- Ensure you are using Unity version `2022.3.48f1` for optimal compatibility.
- All scripts are customizable. Experiment with parameters to better understand how cloth physics is simulated.
- Refer to Unity's official [documentation](https://docs.unity3d.com/) for further details on physics and scripting.

---

## License

This project is distributed for educational purposes. Feel free to modify and expand upon the code for your assignments or personal projects.


