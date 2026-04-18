using UnityEngine;

/// <summary>
/// Marker component placed by <see cref="BridgeBuilder"/> on every runtime
/// bridge piece (intermediate blocks + the two anchor tacks). The presence of
/// this component is the sole signal that an actor (player, enemy) is touching
/// a bridge surface.
///
/// Why a marker MonoBehaviour and not a tag/layer:
/// - Tags require pre-defined entries in ProjectSettings/TagManager.asset; a
///   marker compiles cleanly with no project-settings dependency.
/// - A new physics layer would also require ProjectSettings edits AND careful
///   collision-matrix tweaks. The marker rides on top of the block's existing
///   collider (Default layer) without disturbing collisions with anything else.
///
/// Behaviour layered on top:
/// - <see cref="HumanMovement"/> turns on gravity + refills stamina while in
///   contact with any BridgeWalkable from above. Pressing UP releases the
///   bridge so the existing Climbable-layer climb logic can re-engage.
/// - <see cref="Enemy"/> turns on gravity while standing on a BridgeWalkable
///   (no input override; enemies don't climb on demand).
///
/// Empty by design — all logic lives on the actor side.
/// </summary>
public class BridgeWalkable : MonoBehaviour { }
