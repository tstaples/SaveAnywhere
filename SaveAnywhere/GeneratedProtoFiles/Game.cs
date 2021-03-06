// Generated by the protocol buffer compiler.  DO NOT EDIT!
// source: Game.proto
#pragma warning disable 1591, 0612, 3021
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace SaveAnywhere.SaveData {

  /// <summary>Holder for reflection information generated from Game.proto</summary>
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
  public static partial class GameReflection {

    #region Descriptor
    /// <summary>File descriptor for Game.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static GameReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "CgpHYW1lLnByb3RvGgxDb21tb24ucHJvdG8iHAoMR2FtZUxvY2F0aW9uEgwK",
            "BG5hbWUYASABKAkimAEKBkZhcm1lchIaCghwb3NpdGlvbhgBIAEoCzIILlZl",
            "Y3RvcjISJgoPY3VycmVudExvY2F0aW9uGAIgASgLMg0uR2FtZUxvY2F0aW9u",
            "EhcKD2ZhY2luZ0RpcmVjdGlvbhgDIAEoBRIPCgdzdGFtaW5hGAQgASgCEg4K",
            "BmhlYWx0aBgFIAEoBRIQCghzd2ltbWluZxgGIAEoCCIzCgVHYW1lMRIRCgl0",
            "aW1lT2ZEYXkYASABKAUSFwoGcGxheWVyGAIgASgLMgcuRmFybWVyQhiqAhVT",
            "YXZlQW55d2hlcmUuU2F2ZURhdGFiBnByb3RvMw=="));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { global::SaveAnywhere.SaveData.CommonReflection.Descriptor, },
          new pbr::GeneratedCodeInfo(null, new pbr::GeneratedCodeInfo[] {
            new pbr::GeneratedCodeInfo(typeof(global::SaveAnywhere.SaveData.GameLocation), global::SaveAnywhere.SaveData.GameLocation.Parser, new[]{ "Name" }, null, null, null),
            new pbr::GeneratedCodeInfo(typeof(global::SaveAnywhere.SaveData.Farmer), global::SaveAnywhere.SaveData.Farmer.Parser, new[]{ "Position", "CurrentLocation", "FacingDirection", "Stamina", "Health", "Swimming" }, null, null, null),
            new pbr::GeneratedCodeInfo(typeof(global::SaveAnywhere.SaveData.Game1), global::SaveAnywhere.SaveData.Game1.Parser, new[]{ "TimeOfDay", "Player" }, null, null, null)
          }));
    }
    #endregion

  }
  #region Messages
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
  public sealed partial class GameLocation : pb::IMessage<GameLocation> {
    private static readonly pb::MessageParser<GameLocation> _parser = new pb::MessageParser<GameLocation>(() => new GameLocation());
    public static pb::MessageParser<GameLocation> Parser { get { return _parser; } }

    public static pbr::MessageDescriptor Descriptor {
      get { return global::SaveAnywhere.SaveData.GameReflection.Descriptor.MessageTypes[0]; }
    }

    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    public GameLocation() {
      OnConstruction();
    }

    partial void OnConstruction();

    public GameLocation(GameLocation other) : this() {
      name_ = other.name_;
    }

    public GameLocation Clone() {
      return new GameLocation(this);
    }

    /// <summary>Field number for the "name" field.</summary>
    public const int NameFieldNumber = 1;
    private string name_ = "";
    /// <summary>
    ///  Name of the location (ie. Town)
    /// </summary>
    public string Name {
      get { return name_; }
      set {
        name_ = pb::Preconditions.CheckNotNull(value, "value");
      }
    }

    public override bool Equals(object other) {
      return Equals(other as GameLocation);
    }

    public bool Equals(GameLocation other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (Name != other.Name) return false;
      return true;
    }

    public override int GetHashCode() {
      int hash = 1;
      if (Name.Length != 0) hash ^= Name.GetHashCode();
      return hash;
    }

    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    public void WriteTo(pb::CodedOutputStream output) {
      if (Name.Length != 0) {
        output.WriteRawTag(10);
        output.WriteString(Name);
      }
    }

    public int CalculateSize() {
      int size = 0;
      if (Name.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(Name);
      }
      return size;
    }

    public void MergeFrom(GameLocation other) {
      if (other == null) {
        return;
      }
      if (other.Name.Length != 0) {
        Name = other.Name;
      }
    }

    public void MergeFrom(pb::CodedInputStream input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            input.SkipLastField();
            break;
          case 10: {
            Name = input.ReadString();
            break;
          }
        }
      }
    }

  }

  /// <summary>
  ///  Player data
  /// </summary>
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
  public sealed partial class Farmer : pb::IMessage<Farmer> {
    private static readonly pb::MessageParser<Farmer> _parser = new pb::MessageParser<Farmer>(() => new Farmer());
    public static pb::MessageParser<Farmer> Parser { get { return _parser; } }

    public static pbr::MessageDescriptor Descriptor {
      get { return global::SaveAnywhere.SaveData.GameReflection.Descriptor.MessageTypes[1]; }
    }

    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    public Farmer() {
      OnConstruction();
    }

    partial void OnConstruction();

    public Farmer(Farmer other) : this() {
      Position = other.position_ != null ? other.Position.Clone() : null;
      CurrentLocation = other.currentLocation_ != null ? other.CurrentLocation.Clone() : null;
      facingDirection_ = other.facingDirection_;
      stamina_ = other.stamina_;
      health_ = other.health_;
      swimming_ = other.swimming_;
    }

    public Farmer Clone() {
      return new Farmer(this);
    }

    /// <summary>Field number for the "position" field.</summary>
    public const int PositionFieldNumber = 1;
    private global::SaveAnywhere.SaveData.Vector2 position_;
    public global::SaveAnywhere.SaveData.Vector2 Position {
      get { return position_; }
      set {
        position_ = value;
      }
    }

    /// <summary>Field number for the "currentLocation" field.</summary>
    public const int CurrentLocationFieldNumber = 2;
    private global::SaveAnywhere.SaveData.GameLocation currentLocation_;
    public global::SaveAnywhere.SaveData.GameLocation CurrentLocation {
      get { return currentLocation_; }
      set {
        currentLocation_ = value;
      }
    }

    /// <summary>Field number for the "facingDirection" field.</summary>
    public const int FacingDirectionFieldNumber = 3;
    private int facingDirection_;
    public int FacingDirection {
      get { return facingDirection_; }
      set {
        facingDirection_ = value;
      }
    }

    /// <summary>Field number for the "stamina" field.</summary>
    public const int StaminaFieldNumber = 4;
    private float stamina_;
    public float Stamina {
      get { return stamina_; }
      set {
        stamina_ = value;
      }
    }

    /// <summary>Field number for the "health" field.</summary>
    public const int HealthFieldNumber = 5;
    private int health_;
    public int Health {
      get { return health_; }
      set {
        health_ = value;
      }
    }

    /// <summary>Field number for the "swimming" field.</summary>
    public const int SwimmingFieldNumber = 6;
    private bool swimming_;
    public bool Swimming {
      get { return swimming_; }
      set {
        swimming_ = value;
      }
    }

    public override bool Equals(object other) {
      return Equals(other as Farmer);
    }

    public bool Equals(Farmer other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (!object.Equals(Position, other.Position)) return false;
      if (!object.Equals(CurrentLocation, other.CurrentLocation)) return false;
      if (FacingDirection != other.FacingDirection) return false;
      if (Stamina != other.Stamina) return false;
      if (Health != other.Health) return false;
      if (Swimming != other.Swimming) return false;
      return true;
    }

    public override int GetHashCode() {
      int hash = 1;
      if (position_ != null) hash ^= Position.GetHashCode();
      if (currentLocation_ != null) hash ^= CurrentLocation.GetHashCode();
      if (FacingDirection != 0) hash ^= FacingDirection.GetHashCode();
      if (Stamina != 0F) hash ^= Stamina.GetHashCode();
      if (Health != 0) hash ^= Health.GetHashCode();
      if (Swimming != false) hash ^= Swimming.GetHashCode();
      return hash;
    }

    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    public void WriteTo(pb::CodedOutputStream output) {
      if (position_ != null) {
        output.WriteRawTag(10);
        output.WriteMessage(Position);
      }
      if (currentLocation_ != null) {
        output.WriteRawTag(18);
        output.WriteMessage(CurrentLocation);
      }
      if (FacingDirection != 0) {
        output.WriteRawTag(24);
        output.WriteInt32(FacingDirection);
      }
      if (Stamina != 0F) {
        output.WriteRawTag(37);
        output.WriteFloat(Stamina);
      }
      if (Health != 0) {
        output.WriteRawTag(40);
        output.WriteInt32(Health);
      }
      if (Swimming != false) {
        output.WriteRawTag(48);
        output.WriteBool(Swimming);
      }
    }

    public int CalculateSize() {
      int size = 0;
      if (position_ != null) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(Position);
      }
      if (currentLocation_ != null) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(CurrentLocation);
      }
      if (FacingDirection != 0) {
        size += 1 + pb::CodedOutputStream.ComputeInt32Size(FacingDirection);
      }
      if (Stamina != 0F) {
        size += 1 + 4;
      }
      if (Health != 0) {
        size += 1 + pb::CodedOutputStream.ComputeInt32Size(Health);
      }
      if (Swimming != false) {
        size += 1 + 1;
      }
      return size;
    }

    public void MergeFrom(Farmer other) {
      if (other == null) {
        return;
      }
      if (other.position_ != null) {
        if (position_ == null) {
          position_ = new global::SaveAnywhere.SaveData.Vector2();
        }
        Position.MergeFrom(other.Position);
      }
      if (other.currentLocation_ != null) {
        if (currentLocation_ == null) {
          currentLocation_ = new global::SaveAnywhere.SaveData.GameLocation();
        }
        CurrentLocation.MergeFrom(other.CurrentLocation);
      }
      if (other.FacingDirection != 0) {
        FacingDirection = other.FacingDirection;
      }
      if (other.Stamina != 0F) {
        Stamina = other.Stamina;
      }
      if (other.Health != 0) {
        Health = other.Health;
      }
      if (other.Swimming != false) {
        Swimming = other.Swimming;
      }
    }

    public void MergeFrom(pb::CodedInputStream input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            input.SkipLastField();
            break;
          case 10: {
            if (position_ == null) {
              position_ = new global::SaveAnywhere.SaveData.Vector2();
            }
            input.ReadMessage(position_);
            break;
          }
          case 18: {
            if (currentLocation_ == null) {
              currentLocation_ = new global::SaveAnywhere.SaveData.GameLocation();
            }
            input.ReadMessage(currentLocation_);
            break;
          }
          case 24: {
            FacingDirection = input.ReadInt32();
            break;
          }
          case 37: {
            Stamina = input.ReadFloat();
            break;
          }
          case 40: {
            Health = input.ReadInt32();
            break;
          }
          case 48: {
            Swimming = input.ReadBool();
            break;
          }
        }
      }
    }

  }

  /// <summary>
  ///  Main object containing all save data
  /// </summary>
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
  public sealed partial class Game1 : pb::IMessage<Game1> {
    private static readonly pb::MessageParser<Game1> _parser = new pb::MessageParser<Game1>(() => new Game1());
    public static pb::MessageParser<Game1> Parser { get { return _parser; } }

    public static pbr::MessageDescriptor Descriptor {
      get { return global::SaveAnywhere.SaveData.GameReflection.Descriptor.MessageTypes[2]; }
    }

    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    public Game1() {
      OnConstruction();
    }

    partial void OnConstruction();

    public Game1(Game1 other) : this() {
      timeOfDay_ = other.timeOfDay_;
      Player = other.player_ != null ? other.Player.Clone() : null;
    }

    public Game1 Clone() {
      return new Game1(this);
    }

    /// <summary>Field number for the "timeOfDay" field.</summary>
    public const int TimeOfDayFieldNumber = 1;
    private int timeOfDay_;
    public int TimeOfDay {
      get { return timeOfDay_; }
      set {
        timeOfDay_ = value;
      }
    }

    /// <summary>Field number for the "player" field.</summary>
    public const int PlayerFieldNumber = 2;
    private global::SaveAnywhere.SaveData.Farmer player_;
    public global::SaveAnywhere.SaveData.Farmer Player {
      get { return player_; }
      set {
        player_ = value;
      }
    }

    public override bool Equals(object other) {
      return Equals(other as Game1);
    }

    public bool Equals(Game1 other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (TimeOfDay != other.TimeOfDay) return false;
      if (!object.Equals(Player, other.Player)) return false;
      return true;
    }

    public override int GetHashCode() {
      int hash = 1;
      if (TimeOfDay != 0) hash ^= TimeOfDay.GetHashCode();
      if (player_ != null) hash ^= Player.GetHashCode();
      return hash;
    }

    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    public void WriteTo(pb::CodedOutputStream output) {
      if (TimeOfDay != 0) {
        output.WriteRawTag(8);
        output.WriteInt32(TimeOfDay);
      }
      if (player_ != null) {
        output.WriteRawTag(18);
        output.WriteMessage(Player);
      }
    }

    public int CalculateSize() {
      int size = 0;
      if (TimeOfDay != 0) {
        size += 1 + pb::CodedOutputStream.ComputeInt32Size(TimeOfDay);
      }
      if (player_ != null) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(Player);
      }
      return size;
    }

    public void MergeFrom(Game1 other) {
      if (other == null) {
        return;
      }
      if (other.TimeOfDay != 0) {
        TimeOfDay = other.TimeOfDay;
      }
      if (other.player_ != null) {
        if (player_ == null) {
          player_ = new global::SaveAnywhere.SaveData.Farmer();
        }
        Player.MergeFrom(other.Player);
      }
    }

    public void MergeFrom(pb::CodedInputStream input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            input.SkipLastField();
            break;
          case 8: {
            TimeOfDay = input.ReadInt32();
            break;
          }
          case 18: {
            if (player_ == null) {
              player_ = new global::SaveAnywhere.SaveData.Farmer();
            }
            input.ReadMessage(player_);
            break;
          }
        }
      }
    }

  }

  #endregion

}

#endregion Designer generated code
