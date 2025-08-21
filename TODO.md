# Last Epoch Mod - TODO List

## 🚀 High Priority Features

### 1. Shrine Detection System

**Status**: 🔄 In Progress  
**Complexity**: Medium  
**Dependencies**: ESP system, ObjectManager

**Tasks**:
- [ ] Extend `ObjectManager` to detect shrine entities
- [ ] Add shrine detection logic similar to special enemies (Magic/Rare)
- [ ] Implement shrine classification system in `Settings.cs`
- [ ] Add shrine drawing/ESP functionality
- [ ] Test shrine detection accuracy across different zones

**Implementation Notes**:
- Leverage existing `ShouldDrawShrine()` method in Settings
- Use similar approach to `DisplayActorClass` for shrine types
- Consider shrine states (active/inactive, buffed/unbuffed)

### 2. Combat Damage Screen Flash

**Status**: 📋 Planned  
**Complexity**: Medium  
**Dependencies**: UI hooking system, Drawing system

**Tasks**:
- [ ] Research UI element hooking for damage indicators
- [ ] Implement screen flash overlay system
- [ ] Add damage threshold detection
- [ ] Create configurable flash intensity/duration
- [ ] Add toggle for combat-only vs all damage
- [ ] Test performance impact of overlay rendering

**Implementation Notes**:
- Extend existing UI hooking in `Patches.cs`
- Use `Drawing.cs` for overlay rendering
- Consider using Unity's post-processing or simple overlay
- Add settings for flash color, duration, intensity

### 3. Auto Disconnect on Low Health

**Status**: 📋 Planned  
**Complexity**: Medium  
**Dependencies**: AutoPotion system, ObjectManager

**Tasks**:
- [ ] Extend `AutoPotion.cs` with disconnect logic
- [ ] Add health threshold setting for auto-disconnect
- [ ] Implement potion availability check
- [ ] Add safe disconnect method (quit to menu)
- [ ] Add confirmation/confirmation bypass setting
- [ ] Test in both online and offline modes

**Implementation Notes**:
- Reuse existing health monitoring from AutoPotion
- Add potion inventory checking logic
- Use `OnApplicationQuit` or similar for safe disconnect
- Consider adding delay/grace period before disconnect

## 🎯 Medium Priority Features

### 4. Stash Button in Inventory

**Status**: 📋 Planned  
**Complexity**: High  
**Dependencies**: UI patching system, inventory system

**Tasks**:
- [ ] Research inventory UI structure and components
- [ ] Implement UI element injection for stash button
- [ ] Add button positioning and styling
- [ ] Implement stash opening functionality
- [ ] Add hotkey support for stash button
- [ ] Test button functionality across different inventory states

**Implementation Notes**:
- Study KG's mod implementation for reference
- Use Harmony patches for UI element injection
- Consider button placement (bottom of inventory)
- Add visual feedback for button interactions

### 5. Anti-Idle Kick Prevention

**Status**: 📋 Planned  
**Complexity**: Low-Medium  
**Dependencies**: UI system, timing system

**Tasks**:
- [ ] Research idle detection mechanisms
- [ ] Implement periodic UI interaction (window open/close)
- [ ] Add configurable idle prevention interval
- [ ] Add toggle for anti-idle system
- [ ] Test effectiveness across different game states
- [ ] Add logging for idle prevention events

**Implementation Notes**:
- Simple approach: periodically open/close inventory or character panel
- More sophisticated: simulate mouse movement or key presses
- Consider game state awareness (in combat, in menu, etc.)
- Add settings for prevention method and frequency

## 🔄 On Hold / Future Features

### 6. NPC Icons on Minimap

**Status**: ⏸️ On Hold  
**Complexity**: High  
**Dependencies**: DMMap subsystem (needs update)

**Tasks**:
- [ ] Wait for DMMap subsystem to stabilize after game update
- [ ] Research new minimap icon system
- [ ] Implement NPC icon placement logic
- [ ] Add icon customization options
- [ ] Test performance with multiple NPC icons
- [ ] Add filtering options for different NPC types

**Implementation Notes**:
- Previous DMMap implementation was partially working
- Need to adapt to new game update changes
- Consider performance impact of multiple minimap icons
- May need to wait for game update to settle

## 🛠️ Technical Improvements

### 7. Code Quality & Performance

**Status**: 📋 Ongoing  
**Complexity**: Low-Medium

**Tasks**:
- [ ] Optimize ESP rendering performance
- [ ] Reduce GC pressure in update loops
- [ ] Add comprehensive error handling
- [ ] Implement feature flag system for better modularity
- [ ] Add performance monitoring/logging
- [ ] Refactor duplicate code in ESP systems

### 8. Configuration & Settings

**Status**: 📋 Ongoing  
**Complexity**: Low

**Tasks**:
- [ ] Add MelonPreferences integration for persistent settings
- [ ] Create settings UI for new features
- [ ] Add hotkey configuration system
- [ ] Implement settings validation
- [ ] Add settings import/export functionality
- [ ] Create settings documentation

## 🧪 Testing & Validation

### 9. Testing Framework

**Status**: 📋 Planned  
**Complexity**: Medium

**Tasks**:
- [ ] Create unit tests for core systems
- [ ] Add integration tests for feature interactions
- [ ] Implement automated testing for UI elements
- [ ] Add performance benchmarking
- [ ] Create test scenarios for edge cases
- [ ] Add regression testing for game updates

## 📚 Documentation

### 10. Documentation Updates

**Status**: 📋 Ongoing  
**Complexity**: Low

**Tasks**:
- [ ] Update README with new features
- [ ] Create user guide for new functionality
- [ ] Document API changes and breaking changes
- [ ] Add troubleshooting guide
- [ ] Create developer documentation
- [ ] Add code comments for complex systems

---

## 🎯 Implementation Priority Order

1. **Shrine Detection** - Extends existing ESP system, moderate complexity
2. **Combat Screen Flash** - Uses existing UI hooking, good user value
3. **Auto Disconnect** - Extends AutoPotion, safety feature
4. **Stash Button** - High user value, but complex UI integration
5. **Anti-Idle Prevention** - Simple implementation, good QoL feature
6. **NPC Minimap Icons** - On hold until DMMap system stabilizes

## 🔧 Technical Considerations

- **Performance**: Monitor ESP rendering impact, especially with new features
- **Compatibility**: Test all features in both online and offline modes
- **Safety**: Ensure auto-disconnect doesn't cause data loss
- **User Experience**: Add appropriate toggles and settings for all features
- **Maintainability**: Keep code modular and well-documented

## 📝 Notes

- All new features should include appropriate error handling
- Consider adding feature flags for easy enabling/disabling
- Test thoroughly in different game states and scenarios
- Maintain backward compatibility where possible
- Document any breaking changes or new dependencies
