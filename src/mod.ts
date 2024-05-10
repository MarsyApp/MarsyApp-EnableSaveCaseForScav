import {DependencyContainer} from "tsyringe";
import {ILogger} from "@spt-aki/models/spt/utils/ILogger";
import {ProfileHelper} from "@spt-aki/helpers/ProfileHelper";
import {IPmcData} from "@spt-aki/models/eft/common/IPmcData";
import {ProfileController} from "@spt-aki/controllers/ProfileController";
import {PlayerScavGenerator} from "@spt-aki/generators/PlayerScavGenerator";
import {ItemHelper} from "@spt-aki/helpers/ItemHelper";

class Mod {
	private static logger: ILogger;
	private static itemHelper: ItemHelper;

	preAkiLoad(container: DependencyContainer): void {
		Mod.logger = container.resolve<ILogger>("WinstonLogger");
		Mod.itemHelper = container.resolve<ItemHelper>("ItemHelper");

		Mod.logger.info("[MarsyApp-EnableSaveCaseForScav] preAkiLoad");

		container.afterResolution("ProfileHelper", (_t, result: ProfileHelper) => {
				result.removeSecureContainer = (profile: IPmcData) => {
					const items = profile.Inventory.items;
					const secureContainer = items.find((x) => x.slotId === "SecuredContainer");
					if (secureContainer)
					{
						// Find and remove container + children
						const childItemsInSecureContainer = Mod.itemHelper.findAndReturnChildrenByItems(
							items,
							secureContainer._id,
						);


						// Remove child items + secure container
						profile.Inventory.items = items.filter((x) => secureContainer._id == x._id || !childItemsInSecureContainer.includes(x._id));
					}

					return profile;
				}
			},
			{frequency: "Always"});

		container.afterResolution("ProfileController", (_t, result: ProfileController) => {
				const oldgeneratePlayerScav = result.generatePlayerScav.bind(result);
				result.generatePlayerScav = (sessionID: string) => {
					Mod.logger.info("[MarsyApp-EnableSaveCaseForScav] generatePlayerScav called");
					return oldgeneratePlayerScav(sessionID);
				}
			},
			{frequency: "Always"});

		container.afterResolution("PlayerScavGenerator", (_t, result: PlayerScavGenerator) => {
				const oldgenerate = result.generate.bind(result);
				result.generate = (sessionID: string) => {
					Mod.logger.info("[MarsyApp-EnableSaveCaseForScav] generate called");
					return oldgenerate(sessionID);
				}
			},
			{frequency: "Always"});
	}
}

module.exports = {mod: new Mod()}
