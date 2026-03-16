**DocuBuild: Modular Documentation Engine**

DocuBuild is a high-performance WPF-based desktop application designed for technical writers and developers to build modular, JSON-backed documentation. It bridges the gap between a Live Rich Text Editor and Web-Ready HTML, allowing for seamless publishing to IIS or static web hosts.

🚀 Key Features
**Modular Content Blocks**: Build pages using discrete sections (H1, H2, Paragraphs, Code Blocks, etc.).

**Rich Text Editing:** A modern, slide-up formatting toolbar for real-time text styling.

**Rich Text Pipeline:** Custom-built bi-directional engine that converts WPF FlowDocuments into clean, injectable HTML.

**Developer-Centric Code Blocks:** Specialized dark-themed sections with fixed-width fonts for snippets.

**JSON Persistence:** Saves your entire project structure in a modular JSON format for easy version control.

**IIS Integration:** One-click publishing to local or remote IIS web servers.

🛠 **Tech Stack**
**Framework:** .NET 8 / WPF (C#)

**Architecture:** MVVM (Model-View-ViewModel)

**Data Format:** JSON (System.Text.Json)

**Styling:** XAML with Dynamic Resource Triggers & Segoe MDL2 Assets

📝** Project Status & Roadmap**
While the core engine is functional, the following tasks are currently in the Development Pipeline:

1. **Reverse Reading Refinement** (High Priority)
**Current State:** Successfully converts UI text to HTML tags (<b>, <i>, <span>).

**Pending:** Implementing a robust "Reverse Reader" to perfectly reconstruct complex nested tags (e.g., a bolded link inside a highlighted span) when reloading a saved JSON file into the editor.

2. **HTML Theme Engine**
**Current State:** Generates raw HTML structures.

**Pending:** Development of standardized CSS themes (Light, Dark, and Professional) to ensure the published website looks as polished as the desktop editor.

3. **Rich Media Support**
**Current State:** Placeholders for image sections.

**Pending:** Implementation of a "Drag & Drop" image uploader that handles local file paths and converts them into relative web paths for publishing.

4. **UI/UX Coherence**
**Current State:** Mixed styles between generic WPF controls and custom templates.

**Pending:** Standardizing all action buttons and dialogs to follow a singular design language, ensuring a frictionless user experience from "New Project" to "Publish."

**📥 Getting Started**
>Clone the Repo: git clone https://github.com/yourusername/DocuBuild.git

>Open in VS 2022: Ensure you have the .NET Desktop Development workload installed.

>Build & Run: Press F5 to launch the editor.

>Add Sections: Use the bottom action bar to begin building your first documentation page.

🤝 **Contributing**
This project is in active development. If you are interested in helping with the WPF RichText parsing or CSS Theme templates, feel free to open an issue or submit a pull request!
