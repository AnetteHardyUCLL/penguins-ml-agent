# Penguin-ML-Agent Hunger System 🐧

This project simulates a biologically-inspired AI penguin agent capable of balancing its own survival (hunger management) with parental responsibilities (feeding its baby). Built using Unity ML-Agents.

## Project Structure

- `Assets/Penguins/Scripts/`: C# implementation of agent logic, hunger systems, and environment control.
- `ppo/`: ML-Agents training configuration (`Penguin.yaml`).
- `NNModels/`: Pre-trained `.onnx` models for direct inference.
- `Scenes/`: Unity scene files for running the environment.

## How to run

### 1. Open simulation in Unity

- Install Unity 2021 LTS.
- Clone this repository.
- Open the project and load the scene:  
  `Assets/Scenes/PenguinEnvironment.unity`
- Press **Play** to observe the trained AI behavior.

### 2. [Optional] Manual control

- In the Behavior Parameters, switch Behavior Type to **Heuristic Only**.
- Use `W/A/S/D` to move and **Space** to eat, **F** to feed the baby.

## Report & Documentation

See the full Technical Report for detailed methodology, results, and lessons learned.
