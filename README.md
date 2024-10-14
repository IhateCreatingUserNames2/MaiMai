# NPC Interaction System with AI Agent (LLMUnity Framework)

This requires: LLMUnity and RagSearchUnity Imported
https://github.com/undreamai/LLMUnity
https://github.com/undreamai/RAGSearchUnity

This project is focused on creating an NPC interaction system in Unity using the **LLMUnity** framework. The system allows NPCs to interact with players through AI-driven dialogue, memory management, and context-based responses.

## Key Components

### **AIAgent.cs**

Represents an AI-driven NPC (agent) with a unique `AgentId`, name, personality, and background. Each agent can interact with players, store conversation history, and retrieve context using a memory manager.

- **Key Methods**:
  - `Interact`: Handles player messages, retrieves relevant context using the memory manager, and constructs a prompt for the AI.
  - `RetrieveRelevantContext`: Fetches relevant conversation history or context for better responses.

---

### **PicoDialogue.cs**

Manages the interaction between the player and the NPC, connecting player input to the AI agent and controlling the dialogue UI. This system supports both floating dialogue and Photon Chat integration for multiplayer.

- **Key Methods**:
  - `OpenPlayerInputUI`: Displays the input UI when the player is near an NPC.
  - `RunAsyncResponse`: Sends player input to the AI agent and displays the agent's response.

---

### **AIAgentManager.cs**

Responsible for spawning AI agents, managing agent registration, and linking agents with **PicoDialogue**. It also handles loading chat history and ensures that agents are linked to their memory for context-based responses.

- **Key Methods**:
  - `SpawnAI`: Instantiates an AI agent in the scene and assigns the necessary components for interaction.
  - `LoadAllAgents`: Loads agents from saved data using the **SaveSystem**.

---

### **SaveSystem.cs**

Handles saving and loading AI agents and their conversation histories. It stores agent data, including chat history, using JSON serialization.

- **Key Methods**:
  - `SaveAllAgents`: Saves all registered AI agents and their histories.
  - `LoadAllAgents`: Loads all agents and their chat histories back into the system.

---

### **AgentCreateUI.cs**

Provides a UI for creating new AI agents. Players or developers can input agent names, personalities, and backgrounds to instantiate a new AI agent.

- **Key Methods**:
  - `OnCreateAgentClicked`: Creates a new agent using the provided inputs and registers it with the **AIAgentManager**.

---

### **LLMCharacterMemoryManager.cs**

Manages the memory of each AI agent by embedding and retrieving conversation history for context-based responses. It uses a **SearchEngine** to retrieve relevant conversations.

- **Key Methods**:
  - `EmbedChatHistory`: Embeds chat history into the memory for future retrieval.
  - `SearchChatHistory`: Retrieves relevant past conversations based on the player’s query.

---

### **ModelSelector.cs**

Provides UI functionality to select and switch between different LLM (Language Model) configurations.

- **Key Methods**:
  - `OnChangeModelButtonClicked`: Switches the AI model used by **LLMCharacter** based on player selection.

---

### **GameSettings.cs**

Handles global settings for the game, such as enabling voice-over for the AI responses.

---

## Interaction Flow

1. **Agent Creation**: New agents are created using the **AgentCreateUI** by providing personality and background information.
2. **Agent Registration**: The **AIAgentManager** registers the agent, stores it, and can spawn it in the game world.
3. **Player Interaction**: When a player approaches an AI agent, **PicoDialogue** opens the player input UI, allowing the player to send messages.
4. **AI Response**: The agent uses its memory and context from previous conversations to interact with the player. **LLMCharacter** generates dynamic responses, displayed on the screen.
5. **Saving Data**: Agent data, including chat history, is saved via the **SaveSystem** for persistent interactions across game sessions.

---

## Displaying LLM Output in PicoDialogue

To display the LLM output in **PicoDialogue**, follow these steps:

1. **Set Agent Data**: Assign the correct AI agent using `SetAgentData` when the player interacts with an NPC:
    ```csharp
    picoDialogue.SetAgentData(assignedAgent);
    ```

2. **Player Input Handling**: Process the player's input when they click the send button:
    ```csharp
    public void OnSendButtonClicked()
    {
        string playerInput = playerInputField.text.Trim();
        RunAsyncResponse(playerInput); // Send input for AI processing
    }
    ```

3. **Run Async Response**: Send the player's message to the AI, which generates a response using its memory:
    ```csharp
    private async void RunAsyncResponse(string playerInput)
    {
        string aiResponse = await currentAgent.Interact(userId, playerInput);
        ShowDialogue(aiResponse); // Display the response
    }
    ```

4. **Show Dialogue**: Display the AI's response in both the dialogue UI and floating text:
    ```csharp
    public void ShowDialogue(string message)
    {
        dialogueText.text = message; // Display in UI
        floatingDialogueText.text = message; // Display floating text
    }
    ```

This system provides seamless NPC interaction where the AI generates dynamic, context-aware responses based on the player's input and the agent’s memory.

---

Feel free to explore and modify these components as per your project needs!
