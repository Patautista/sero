# Copilot Instructions

## Project Guidelines
- Use CompanionPromptBuilder as the centralized prompt builder for all prompt-related behavior, including activity-agent prompts; do not use ActivityPromptBuilder.
- For shared UI behavior, prefer extracting reusable methods into a shared Razor base component and having applicable views inherit it.
- Use Tailwind utility classes for styling instead of adding custom CSS rules when working in this MAUI Blazor project.

## Configuration Guidelines
- For mobile Google Cloud Text-to-Speech configuration, use a durable credential injection method instead of a fixed short-lived OAuth access token.