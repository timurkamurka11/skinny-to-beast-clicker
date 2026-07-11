# Skinny to Beast — project workflow

These rules are mandatory for future patches in this repository.

1. When the user says to make a patch, implement the patch completely and upload all code and assets directly to `main` in GitHub.
2. The user should only need to run:

   ```bash
   git pull origin main
   ```

3. Do not ask the user to manually create, copy, rename, or place patch files when GitHub access is available.
4. Do not use generic or standard Unity UI templates when the user provides a reference.
5. Match supplied references as closely as possible: composition, dimensions, spacing, colors, borders, icons, visual hierarchy, and interactive areas.
6. Preserve approved backgrounds and screens. Add only requested changes.
7. UI shown in baked images or video should use precisely aligned transparent hotspots or real controls without covering the approved visual.
8. Synchronize supplied UI sounds with the exact actions named by the user.
9. Do not claim that Unity compilation or a build passed unless it was actually run and verified.
10. After every patch, report the Git commit and tell the user to run only `git pull origin main`.
