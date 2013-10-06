datablock PlayerData(LightMaleBiodermDummy) : LightMaleHumanArmor
{
   shapeFile = "bioderm_light.dts";
   jetEmitter = BiodermArmorJetEmitter;
   jetEffect =  BiodermArmorJetEffect;


   debrisShapeName = "bio_player_debris.dts";

   //Foot Prints
   decalData   = LightBiodermFootprint;
   decalOffset = 0.3;

   waterBreathSound = WaterBreathBiodermSound;

   canObserve = false;
   groundImpactMinSpeed    = 0.01;
   minImpactSpeed = 5;
   speedDamageScale = 0.004;


};
function LightMaleBiodermDummy::damageObject(%data, %targetObject, %sourceObject, %position, %amount, %damageType, %momVec, %mineSC)
{


   if(%targetObject.DummyState !$= "hit")
      {

      %sourceClient = isObject(%sourceObject) ? %sourceObject.getOwnerClient() : 0;

      messageClient(%sourceClient, 'ChatMessage', '~wgui/Objective_notification.wav');  //LaunchMenuOver
      %targetObject.blowup();

      %fadeTime = 1000;
      %targetObject.startFade( %fadeTime, ($CorpseTimeoutValue) - %fadeTime, true );
      %targetObject.schedule(100, "delete");
      %targetObject.DummyState = "hit";
      if(%sourceObject.onSnipingPad)
         %SourceObject.Client.SniperHits++;
         DisplaySniperScore(%SourceObject.Client);
      }
}


////////////////////////////////////////////////////////////////////////////////
//                           Sniper Station Data                              //
////////////////////////////////////////////////////////////////////////////////
datablock EffectProfile(SnipingPadAcitvateEffect)
{
   effectname = "powered/vehicle_screen_on2";
   minDistance = 3.0;
   maxDistance = 5.0;
};
datablock EffectProfile(SnipingPadDeactivateEffect)
{
   effectname = "powered/vehicle_screen_off";
   minDistance = 3.0;
   maxDistance = 5.0;
};
datablock AudioProfile(SnipingPadAcitvateSound)
{
   filename    = "fx/powered/vehicle_screen_on2.wav";
   description = AudioClosest3d;
   preload = true;
   effect = SnipingPadAcitvateEffect;
};
datablock AudioProfile(SnipingPadDeactivateSound)
{
   filename    = "fx/powered/vehicle_screen_off.wav";
   description = AudioClose3d;
   preload = true;
   effect = SnipingPadDeactivateEffect;
};

datablock StaticShapeData(SnipingPad) : StaticShapeDamageProfile
{
   className = Station;
   catagory = "Stations";
   shapeFile = "vehicle_pad_station.dts";
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
function SnipingPad::getSound(%data, %forward)
{
   if(%forward)
      return "SnipingPadAcitvateSound";
   else
      return "SnipingPadDeactivateSound";
}

function SnipingPad::onAdd(%this, %obj)
{
   Parent::onAdd(%this, %obj);
  %obj.playThread($AmbientThread, "Power");

   %obj.setRechargeRate(%obj.getDatablock().rechargeRate);

   %trigger = new Trigger()
   {
      dataBlock = SnipingPadTrigger;
      polyhedron = "-0.75 0.75 0.1 1.5 0.0 0.0 0.0 -1.5 0.0 0.0 0.0 2.3";
   };
   MissionCleanup.add(%trigger);
   %trigger.setTransform(%obj.getTransform());

   %trigger.station = %obj;
   %trigger.mainObj = %obj;
   %trigger.disableObj = %obj;
   %obj.trigger = %trigger;

}
//Decription -- Called when station has been triggered and animation is
//              completed
function SnipingPad::stationReady(%data, %obj)
{
//avoid errors
}
function SnipingPad::EndofOptions(%client)
{
  // error("calling END OF OPTIONS 2 ");
//   error("client is =" SPC %client);
//   %client.player.setInventory(SniperRifle,1);
//   %client.player.use(SniperRifle);
}
function SnipingPad::stationFinished(%data, %obj)
{
   //Hide the Inventory Station GUI
}
function SnipingPad::onEndSequence(%data, %obj, %thread)
{
  // error("SnipingPad::onEndSequence");
   if(%thread == $ActivateThread)
   {
      %obj.ready = true;
      %obj.stopThread($ActivateThread);
   }
   Parent::onEndSequence(%data, %obj, %thread);
}
function SnipingPad::Exit(%player)
{
   %player.setMoveState(false);

}
////////////////////////////////////////////////////////////////////////////////
//                         Sniping Pad Trigger Data                           //
////////////////////////////////////////////////////////////////////////////////

datablock TriggerData(SnipingPadTrigger)
{
   tickPeriodMS = 30;
};






function SnipingPadTrigger::onEnterTrigger(%data, %obj, %colObj)
{

   // error("SnipingPadTrigger::onEnterTrigger");

 //make sure it's a player object, and that that object is still alive
   if(%colObj.getDataBlock().className !$= "Armor" || %colObj.getState() $= "Dead")
      return;
   if(%obj.occupied)
      return;

   %obj.station.setThreadDir($ActivateThread, TRUE);
   %obj.station.playThread($ActivateThread,"activate");
  // %obj.station.schedule(100,"playThread",$ActivateThread,"activate");
   %obj.station.playAudio($ActivateSound, %obj.station.getDataBlock().getSound(true));

    %colObj.station = %obj.station;
   %obj.station.triggeredBy = %colObj;
   
   
   %colObj.onSnipingPad = true;
 //  %obj.station.inUse = true;
   %obj.occupied = true;


   //Set players position etc.
         %posXY = getWords(%obj.getTransform(),0 ,1);
         %posZ = getWord(%obj.getTransform(), 2);
         %rotZ =  getWord(%obj.station.getTransform(), 5);
         %angle =  getWord(%obj.station.getTransform(), 6);

         if(%angle > 6.283185308)
            %angle = %angle - 6.283185308;
         %colObj.setvelocity("0 0 0");
         %colObj.setTransform(%posXY @ " " @ %posZ + 0.2 @ " " @ "0 0 "  @ %rotZ @ " " @ %angle );



//   %colObj.lastWeapon = ( %colObj.getMountedImage($WeaponSlot) == 0 ) ? "" : %colObj.getMountedImage($WeaponSlot).getName().item;
//   %colObj.unmountImage($WeaponSlot);
     %colObj.use("SniperRifle");
     %colObj.setMoveState(true);

//     %obj.station.getDataBlock().stationTriggered(%obj.station, 1);

  IGOptions::Start(%colObj.client, "Sniper");

      //Set Sensor Group
      SetTargetSensorGroup(%colObj.Client.Target, 0);
      %colObj.Client.SetSensorGroup(0);
      
      %colObj.Client.SniperHits = 0;
      %colObj.Client.SniperShots = -1;  //Since you press fire to select the mode...
}

function SnipingPadTrigger::onLeaveTrigger(%data, %obj, %colObj)
{
  //error("SnipingPadTrigger::onLeaveTrigger");
   if(%colObj.getDataBlock().className !$= "Armor" || !%colObj.onSnipingPad)
      return;


//   %obj.station.inUse = false;
   %obj.occupied = false;
   %colObj.onSnipingPad = false;
   
   %colObj.station = "";


   if(%obj.station)
   {
      if(%obj.station.triggeredBy == %colObj)
      {
         error("check ok");
         
         %obj.station.getDataBlock().stationFinished(%obj.station);
         %obj.station.getDataBlock().endRepairing(%obj.station);
         %obj.station.triggeredBy = "";
         %obj.station.getDataBlock().stationTriggered(%obj.station, 0);

         if(!%colObj.teleporting)
            %colObj.station = "";

      }
   }

      //Set Sensor Group
      SetTargetSensorGroup(%colObj.Client.Target, 1);
      %colObj.Client.SetSensorGroup(1);
  // Error("Exiting Trigger");
      clearBottomPrint( %colObj.client );
}

function SnipingPad::stationTriggered(%data, %obj, %isTriggered)
{
  // error("SnipingPad::stationTriggered");

   if(%isTriggered)
   {
      %obj.setThreadDir($ActivateThread, TRUE);
      %obj.playThread($ActivateThread,"activate");
      %obj.playAudio($ActivateSound, %data.getSound(true));
      %obj.inUse = "Up";
   }
   else
   {
      if(%obj.getDataBlock().getName() !$= StationVehicle)
      {
         %obj.stopThread($ActivateThread);
         if(%obj.getObjectMount())
            %obj.getObjectMount().stopThread($ActivateThread);
         %obj.inUse = "Down";
      }
      else
      {
         %obj.setThreadDir($ActivateThread, FALSE);
         %obj.playThread($ActivateThread,"activate");
         %obj.playAudio($ActivateSound, %data.getSound(false));
         %obj.inUse = "Down";
      }
   }
}
function SnipingPadTrigger::onTickTrigger(%data, %obj)
{
}
////////////////////////////////////////////////////////////////////////////////
//                            Misc. Functions                                 //
////////////////////////////////////////////////////////////////////////////////
function DisplaySniperScore(%client)
{
  %hits = %client.sniperhits;
  %shots = %client.snipershots;
  %acc = mfloor(%hits / %shots * 100);

  %message = "Shots:" SPC %shots @ "    " @ "Hits:" SPC %hits @ "    " @ "Accuracy:" SPC %acc SPC "%";
  bottomPrint( %client, %message, 30, 1 );

}

function DummySpawnDelay(%client)
{
   %client.dummywait = false;
   //error("setting dummywait to false");
}
function SnipingPad::SpawnTarget(%client)
{
// Avoide Dummy spamage
   if(%client.dummyWait)
      return;
   %client.dummyWait = true;
   schedule(1000, 0, "DummySpawnDelay", %client);





   %zoffset = 10;
   %spawndist = 10;
   %upforce = 10;
   %scale = 4000;

   %randdegree = mFloor(getRandom(0, %client.SnipingDiff)) / 2;
   %plusminus = mFloor(getRandom(0, 1));

   %obj = %client.player.station;
   %originx = getword(%obj.getTransform(), 0);
   %originy = getword(%obj.getTransform(), 1);
   %originz = getword(%obj.getTransform(), 2);
   %originzRotRAD = getword(%obj.getTransform(), 6);

   %originzRotDEG = mRadToDeg(%originzRotRAD);
   if(getword(%obj.getTransform(), 5) < 0)
      %originzRotDEG = 360 - %originzRotDEG;

   %originAbsRotDEG = (%originzRotDEG * -1) + 90;
//--------------------------------------------------------------
   if(%plusminus == 0)
      %originAbsRotDEG = %originAbsRotDEG + %randdegree;
   if(%plusminus == 1)
      %originAbsRotDEG = %originAbsRotDEG - %randdegree;

   %relx = mCos(mDegToRad(%originAbsRotDEG)) * %spawndist;
   %rely = mSin(mDegToRad(%originAbsRotDEG)) * %spawndist;

   %spawnx = %relx + %originx;
   %spawny = %rely + %originy;
   %spawnz = %zoffset + %originz;
   %spawnpos = %spawnx SPC %spawny SPC %spawnz;// SPC getword(%obj.getTransform(), 3) SPC getword(%obj.getTransform(), 4) SPC getword(%obj.getTransform(), 5) SPC %originzRotRAD;

   %vec = %relx SPC %rely SPC %upforce;    //%relx SPC %rely SPC %upforce;
   %scaleVec = VectorScale(VectorNormalize(%vec), %scale); //(%scale * -1)



   %dummy = new player() {
   dataBlock = "LightMaleBiodermDummy";
   };

   %newzval = getword(%obj.getTransform(), 2) + %zoffset;
   %newtrans = getword(%obj.getTransform(), 0) SPC getword(%obj.getTransform(), 1) SPC %newzval SPC getword(%obj.getTransform(), 3) SPC getword(%obj.getTransform(), 4) SPC getword(%obj.getTransform(), 5) SPC getword(%obj.getTransform(), 6);

   %dummy.setTransform(%newtrans);
   %dummy.owner = %client;

   %dummy.applyImpulse(%obj.getTransform(), %scaleVec);
   //settargetsensorGroup(%dummy.Target, 2);
}

