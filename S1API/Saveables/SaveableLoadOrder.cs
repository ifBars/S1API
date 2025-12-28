namespace S1API.Saveables
{
    /// <summary>
    /// Defines when a modded saveable should load relative to base game saveables.
    /// <para>
    /// Override the <see cref="S1API.Internal.Abstraction.Saveable.LoadOrder"/> property in your
    /// <see cref="S1API.Internal.Abstraction.Saveable"/> class to control load timing.
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// public class MyCustomSaveable : Saveable
    /// {
    ///     // Override to load BEFORE base game entities
    ///     public override SaveableLoadOrder LoadOrder => SaveableLoadOrder.BeforeBaseGame;
    ///     
    ///     [SaveableField("my_data")]
    ///     private MyDataClass _myData = new MyDataClass();
    /// }
    /// </code>
    /// </example>
    public enum SaveableLoadOrder
    {
        /// <summary>
        /// Load before base game saveables. Runs as a prefix before BuildingsLoader.Load (one of the earliest loaders).
        /// <para>
        /// Use this when your mod data needs to be available before base game ISaveables load, such as:
        /// </para>
        /// <list type="bullet">
        /// <item><description>Setting up hooks that intercept base game load events</description></item>
        /// <item><description>Initializing global state that base game loaders depend on</description></item>
        /// <item><description>Advanced modding scenarios requiring early initialization</description></item>
        /// </list>
        /// <para>
        /// <strong>Important:</strong> When using BeforeBaseGame, base game entities (NPCs, buildings, vehicles) 
        /// are <strong>not yet loaded</strong> when <see cref="S1API.Internal.Abstraction.Saveable.OnLoaded"/> is called.
        /// </para>
        /// </summary>
        BeforeBaseGame,

        /// <summary>
        /// Load after base game saveables (default). Runs as a postfix after NPCsLoader.Load (one of the last loaders).
        /// <para>
        /// Use this when your mod data depends on base game entities being loaded first, such as:
        /// </para>
        /// <list type="bullet">
        /// <item><description>Storing references to NPCs, buildings, or vehicles</description></item>
        /// <item><description>Modifying base game entity states after they're loaded</description></item>
        /// <item><description>Most general-purpose mod saveables</description></item>
        /// </list>
        /// <para>
        /// This is the <strong>default behavior</strong>. If you don't override LoadOrder, your saveable will use AfterBaseGame.
        /// Base game entities <strong>are loaded</strong> when <see cref="S1API.Internal.Abstraction.Saveable.OnLoaded"/> is called.
        /// </para>
        /// </summary>
        AfterBaseGame
    }
}
