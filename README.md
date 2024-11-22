
# MaiMai AI Agent System

**MaiMai AI Agent System** allows players to spawn personal AI agents running through LLMUnity with a **custom AI agent name** and **custom AI agent system prompt**. Additionally, you can create **fixed AI agents** in the scene with customizable names and system prompts.

---

## Requirements

### **LLMUnity**
- GitHub Repository: [https://github.com/undreamai/LLMUnity](https://github.com/undreamai/LLMUnity)

### **PuerTS for JS and LangGraph**
- GitHub Repository: [https://github.com/Tencent/puerts/blob/master/doc/unity/en/install.md](https://github.com/Tencent/puerts/blob/master/doc/unity/en/install.md)
- **Note:** If you're not planning to use LangGraph, comment out the entire `InitializePuerts.cs`.

---

## Configuration for LangGraphJS

This configuration of MaiMai runs with LangGraphJS. However, you can run MaiMai **without LangGraph** by editing the `OnSendButtonClicked()` function in `PicoDialogue.cs`:

### **Modify the following line:**
```csharp
InitializePuerts.Instance.ProcessUserInput(playerInput, currentAgent.AgentId);
```

### **Change it to:**
```csharp
RunAsyncResponse(playerInput);
```

---

## Installation

### **Download Contents:**

1. Extract `MaiMai` to your Unity **Assets** folder.
2. Extract `Resources` to your Unity **Assets/Resources** folder.

---

### **Framework Details:**

- MaiMai was constructed around a **Third Person Shooter Template (MFPS)**.

---

### **Default UI Configuration:**

For user interaction, MaiMai requires a UI with fields like:
- **AI Agent Name**
- **System Prompt**
- **Drop-down list of created agents**
- **Buttons to Spawn and Remove agents**

Utilities like changing models and voices are included but not fully integrated yet.

---

## Fixed NPCs

If adding fixed NPCs to your scene:
- The UI is **not required**.
- Agent Name and System Prompt can be configured by attaching `AiAgentInteraction.cs` to the NPC GameObject.

### **Important:**
- Every prefab must include the `AiAgentInteraction.cs` script.
- Toggle the **Fixed Agent** option in the inspector.

---

## Required Scene Components

Ensure the following components are added to the scene and properly configured in the Unity Inspector:

1. **`AgentCreateUI.cs`** (For user panel)
2. **`PicoDialogue.cs`** (Handles user input UI and LLM response display)
   - Add the `PicoDialogue.onSendButtonClicked()` method to the button's `onClick()` event.
   - Leave NPC configurations blank for dynamic agents.
3. **`LLmCharacter.cs`** (Leave NPC configurations blank for dynamic agents)
4. **`LLM.cs`**
   - Remember to download models and RAG models.
   - **Note:** Some Android utility scripts hardcode models (e.g., 1b and 3B). If adding more, adjust `LLMUnitySetupHelper.cs` accordingly.
5. **`AIManager.cs`**
6. **`AIAgentManager.cs`**
   - Configure in the Inspector.
   - **Current limitation:** Only supports one prefab model for user-created agents (dynamic support planned).
   - Ensure every prefab includes `AiAgentInteraction.cs` and toggles the Fixed Agent option.
7. **`ModelSelector.cs`** (Optional: Load this script if you want to try the model selector.)
8. **`LLMCharacterMemoryManager.cs`**
9. **`RAG.cs`**
   - Configure `SearchType`, `Chunking` (e.g., Token Splitter), and `NumToken` (default: 10).
   - For custom configurations, consult `LLmCharacterMemoryManager.cs`.
10. **`InitializePuerts.cs`** (Initializes the LangGraph system)

---

## Using LangGraph

### **Setup:**

1. **Install Required Dependencies:**
   - Install **TypeScript**, **npx**, and **webpack**.
   - Check `package.json` in `MaiMai/Package/webpack/langgraph-bundler`.

2. **Edit LangGraph Code:**
   - Modify `index.ts` as needed.

3. **Bundle LangGraph:**
   - Run the following command in the `MaiMai/Package/webpack/langgraph-bundler` folder:
     ```bash
     npx webpack
     ```

4. **Update Project:**
   - Copy the generated `langgraph.bundle.mjs` from the `/dist/` folder.
   - Paste it into `ProjectFolder/Assets/Resources/`.

---

## LangGraph Logic

### **User Input Flow:**
1. `InitializePuerts.cs` initializes the JS environment.
2. Runs `testLangGraph.mjs -> langgraph.bundle.mjs -> index.ts`.
3. Imports LangGraph and initializes the environment.

### **Agent Creation:**
```javascript
const GraphState = Annotation.Root();
```

### **Process Flow:**
1. Processes user input, invokes LangGraph, and handles the final state.
2. Passes the final state to:
   ```csharp
   PicoDialogue.Instance.RunAsyncResponse(message);
   ```
3. `AIAgent` interacts with `RAG` to build the final prompt.

---

## Final Prompt Structure

The system constructs the following prompt:
```
Your name is: {AgentName}
Your Custom Prompt: {systemPrompt}

{contextSection}

As {AgentName}, please provide an appropriate response to the user's last message.

Respond in first person and do not include any conversation markers or role labels in your response.

Conversation History:
[ Chat history added from llmcharacter.chat feature ]
```

---

### **Static Completion:**
Replace:
```csharp
llmCharacter.Chat(message);
```
With:
```csharp
llmCharacter.Complete(message);
```
In `AiAgent.cs` for static completion.

---

## License

Refer to the respective repositories for licensing details of the required dependencies.
