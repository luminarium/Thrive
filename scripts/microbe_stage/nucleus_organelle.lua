--------------------------------------------------------------------------------
-- Class for the single core organelle of any microbe
--------------------------------------------------------------------------------
class 'NucleusOrganelle' (OrganelleComponent)

-- See organelle_component.lua for more information about the 
-- organelle component methods and the arguments they receive.

-- Constructor
function NucleusOrganelle:__init(arguments, data)
    OrganelleComponent.__init(self, arguments, data)

    --making sure this doesn't run when load() is called
    if arguments == nil and data == nil then
        return
    end

    self.golgi = Entity()
	self.ER = Entity()
    return self
end

-- Overridded from Organelle:onAddedToMicrobe
function NucleusOrganelle:onAddedToMicrobe(microbe, q, r, rotation, organelle)
    local x, y = axialToCartesian(q-1, r-1)
    local sceneNode1 = OgreSceneNodeComponent()
    sceneNode1.meshName = "golgi.mesh"
	sceneNode1.transform.position = Vector3(x,y,0)
    sceneNode1.transform.scale = Vector3(HEX_SIZE, HEX_SIZE, HEX_SIZE)
    sceneNode1.transform.orientation = Quaternion(Radian(Degree(rotation)), Vector3(0, 0, 1))
    sceneNode1.transform:touch()
    sceneNode1.parent = microbe.entity
    microbe.entity:addChild(self.golgi)
    self.golgi:addComponent(sceneNode1)
	self.golgi.sceneNode = sceneNode1
	self.golgi:setVolatile(true)
	
	local sceneNode2 = OgreSceneNodeComponent()
    sceneNode2.meshName = "ER.mesh"
	sceneNode2.transform.position = Vector3(0,0,0)
    sceneNode2.transform.scale = Vector3(HEX_SIZE, HEX_SIZE, HEX_SIZE)
    sceneNode2.transform.orientation = Quaternion(Radian(Degree(rotation+10)), Vector3(0, 0, 1))
    sceneNode2.transform:touch()
	sceneNode2.parent = microbe.entity
    microbe.entity:addChild(self.ER)
    self.ER:addComponent(sceneNode2) 
	self.ER.sceneNode = sceneNode2
	self.ER:setVolatile(true)
    
    self.sceneNode = organelle.sceneNode
    
    -- If we are not in the editor, get the color of this species.
    if microbe:getSpeciesComponent() ~= nil then
        local speciesColour = microbe:getSpeciesComponent().colour
        self.colourSuffix = "" .. math.floor(speciesColour.x * 256) .. math.floor(speciesColour.y * 256) .. math.floor(speciesColour.z * 256)
    end
end

function NucleusOrganelle:onRemovedFromMicrobe(microbe, q, r)
    self.golgi:destroy()
    self.ER:destroy()
end

function NucleusOrganelle:load(storage)
    self.golgi = Entity()
	self.ER = Entity()
end

function NucleusOrganelle:updateColour(organelle)
    -- Update the colours of the additional organelle models.
    --[[if self.sceneNode.entity ~= nil and self.golgi.sceneNode.entity ~= nil then
        --print(organelle.colour.r .. ", " .. organelle.colour.g .. ", " .. organelle.colour.b)
    
		local entity = self.sceneNode.entity
        local golgiEntity = self.golgi.sceneNode.entity
        local ER_entity = self.ER.sceneNode.entity
        
        entity:tintColour("nucleus", organelle.colour)
        golgiEntity:tintColour("golgi", organelle.colour)
        ER_entity:tintColour("ER", organelle.colour)
        
        organelle._needsColourUpdate = false
    end]]
end
