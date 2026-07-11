# Skinny to Beast — project workflow

These rules are mandatory for future patches in this repository.

1. When the user says `делай патч`, implement the requested patch completely and upload all code and assets directly to `main` in GitHub.
2. Never ask the user to download a ZIP, manually copy files, rename files, or place files into Unity when GitHub access is available.
3. The user should only need to run:

   ```bash
   git pull origin main
   ```

4. Do not tell the user to `git push` after an assistant-created patch, because the assistant already uploaded the changes to GitHub. The user only pulls them locally.
5. Do not use generic or standard Unity UI templates when the user provides a reference.
6. Match supplied references as closely as possible: composition, dimensions, spacing, colors, borders, icons, visual hierarchy, and interactive areas.
7. Preserve approved backgrounds and screens. Add only requested changes.
8. UI shown in baked images or video should use precisely aligned transparent hotspots or real controls without covering the approved visual.
9. Synchronize supplied UI sounds with the exact actions named by the user.
10. Do not claim that Unity compilation or a build passed unless it was actually run and verified.
11. After every patch, report the Git commit and tell the user to run only `git pull origin main`.
