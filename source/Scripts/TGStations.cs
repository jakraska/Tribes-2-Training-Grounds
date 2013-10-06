//datablock EffectProfile(StationInventoryActivateEffect)
//datablock AudioProfile(StationInventoryActivateSound)
//datablock AudioProfile(StationInventoryHumSound)
//datablock StaticShapeData(StationInventory) : StaticShapeDamageProfile


//function StationInventory::onAdd(%this, %obj)
//function StationInventory::stationReady(%data, %obj)
//function StationInventory::beginPersonalInvEffect( %data, %obj )
//function StationInventory::stationFinished(%data, %obj)
//function StationInventory::getSound(%data, %forward)
//function StationInventory::setPlayersPosition(%data, %obj, %trigger, %colObj)

datablock StaticShapeData(TSSelector) : StaticShapeDamageProfile
{
   className = Station;
   catagory = "Stations";
   shapeFile = "Nexusbase.dts";
   maxDamage = 1.00;
   destroyedLevel = 1.00;
   disabledLevel = 0.70;
   explosion      = ShapeExplosion;
	expDmgRadius = 8.0;
	expDamage = 0.4;
	expImpulse = 1500.0;
	// don't allow this object to be damaged in non-team-based
	// mission types (DM, Rabbit, Bounty, Hunters)
	noIndividualDamage = true;

   dynamicType = $TypeMasks::StationObjectType;
	isShielded = true;
	energyPerDamagePoint = 75;
	maxEnergy = 50;
	rechargeRate = 0.35;
   doesRepair = true;
   humSound = StationInventoryHumSound;

   cmdCategory = "Support";
   cmdIcon = CMDStationIcon;
   cmdMiniIconName = "commander/MiniIcons/com_inventory_grey";
   targetNameTag = 'Inventory';
   targetTypeTag = 'Station';

   debrisShapeName = "debris_generic.dts";
   debris = StationDebris;
};

function TSSelector::onAdd(%this, %obj)
{
   if($TSSelector::Count $= "")
      $TSSelector::Count = 0;

   $TSSelector::PadNumber[$TSSelector::Count] = %obj.PadNumber;
   $TSSelector::PadID[$TSSelector::Count] = %obj;

   $TSSelector::Count++;


   Parent::onAdd(%this, %obj);
   %obj.playThread($AmbientThread, "ambient");

   %obj.setRechargeRate(%obj.getDatablock().rechargeRate);

   %trigger = new Trigger()
   {
      dataBlock = TSSelectorTrigger;
      polyhedron = "-0.75 0.75 0.1 1.5 0.0 0.0 0.0 -1.5 0.0 0.0 0.0 2.3";
   };
   MissionCleanup.add(%trigger);
   %trigger.setTransform(%obj.getTransform());

   %trigger.station = %obj;
   %trigger.mainObj = %obj;
   %trigger.disableObj = %obj;
   %obj.trigger = %trigger;

}

///////////// I did not do anything ot this yet...
//Decription -- Called when station has been triggered and animation is
//              completed
function TSSelector::stationReady(%data, %obj)
{
   //Display the Inventory Station GUI here

//   %obj.notReady = 1;
//   %obj.inUse = "Down";     //yes
//   %obj.schedule(500,"playThread",$ActivateThread,"flash");
//   %player = %obj.triggeredBy;
//   %energy = %player.getEnergyLevel();
//   %player.setCloaked(true);
//   %player.schedule(500, "setCloaked", false);
 //  %data.schedule( 500, "beginPersonalInvEffect", %obj );
}
function TSSelector::stationFinished(%data, %obj)
{
   //Hide the Inventory Station GUI
}
function TSSelector::setPlayersPosition(%data, %obj, %trigger, %colObj)
{
    //no longer needed

     %vel = getWords(%colObj.getVelocity(), 0, 1) @ " 0";
   if((VectorLen(%vel) < 22) && (%obj.triggeredBy != %colObj))
   {
      %pos = %trigger.position;
     // %colObj.setvelocity("0 0 0");
	   %rot = getWords(%colObj.getTransform(),3, 6);
     // %colObj.setTransform(getWord(%pos,0) @ " " @ getWord(%pos,1) @ " " @ getWord(%pos,2) + 0.8 @ " " @ %rot);//center player on object
     // %colObj.setMoveState(true);
    //  %colObj.schedule(1600,"setMoveState", false);
    //  %colObj.setvelocity("0 0 0");
      return true;
   }
   return false;
}
function TSSelector::StationSparkEmitter(%client)
{
   if (isObject(%client.StationSparkEmitter))
      %client.StationSparkEmitter.delete();

   %client.StationSparkEmitter = new ParticleEmissionDummy()
   {
      //position = getWord(%client.player.position, 0) SPC getWord(%client.player.position, 1) SPC getWord(%client.player.position, 2) + 3;
      position = (%client.player.getWorldBoxCenter());
      rotation = "1 0 0 0";
      scale = "1 1 1";
      dataBlock = defaultEmissionDummy;
      emitter = (NexusParticleCapEmitter);
      velocity = "1";
   };
   MissionCleanup.add(%client.StationSparkEmitter);

   //the effect should only last a few seconds

   %client.StationSparkEmitter.schedule(200, "delete");
}
