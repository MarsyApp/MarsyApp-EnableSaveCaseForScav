import {DependencyContainer} from "tsyringe";
import {ILogger} from "@spt-aki/models/spt/utils/ILogger";
import {ProfileHelper} from "@spt-aki/helpers/ProfileHelper";
import {IPmcData} from "@spt-aki/models/eft/common/IPmcData";
import {ItemHelper} from "@spt-aki/helpers/ItemHelper";
import {ISaveProgressRequestData} from "@spt-aki/models/eft/inRaid/ISaveProgressRequestData";
import {InraidCallbacks} from "@spt-aki/callbacks/InraidCallbacks";
import {PlayerRaidEndState} from "@spt-aki/models/enums/PlayerRaidEndState";

class Mod {
	private static logger: ILogger;
	private static itemHelper: ItemHelper;

	preAkiLoad(container: DependencyContainer): void {
		Mod.logger = container.resolve<ILogger>("WinstonLogger");
		Mod.itemHelper = container.resolve<ItemHelper>("ItemHelper");

		container.afterResolution("ProfileHelper", (_t, result: ProfileHelper) => {
				result.removeSecureContainer = (profile: IPmcData) => {
					const items = profile.Inventory.items;
					const secureContainer = items.find((x) => x.slotId === "SecuredContainer");
					if (secureContainer) {
						// Find and remove container + children
						const childItemsInSecureContainer = Mod.itemHelper.findAndReturnChildrenByItems(
							items,
							secureContainer._id
						);

						secureContainer._tpl = "5732ee6a24597719ae0c0281";
						// Remove child items + secure container
						profile.Inventory.items = items.filter((x) => secureContainer._id === x._id || !childItemsInSecureContainer.includes(x._id));
					}

					return profile;
				}
			},
			{frequency: "Always"});

		container.afterResolution("InraidCallbacks", (_t, result: InraidCallbacks) => {
			const oldSaveProgress = result.saveProgress.bind(result);
			result.saveProgress = (url: string, info: ISaveProgressRequestData, sessionID: string) => {

				const statusOnExit = info.exit;
				const isScav = info.isPlayerScav;
				const isDead = statusOnExit !== PlayerRaidEndState.SURVIVED && statusOnExit !== PlayerRaidEndState.RUNNER
				if (isScav && isDead) {
					const inventory = info.profile.Inventory;
					const items = inventory.items;
					const secureContainer = items.find((x) => x.slotId === "SecuredContainer");
					if (secureContainer) {
						// Find and remove container + children
						const childItemsInSecureContainer = Mod.itemHelper.findAndReturnChildrenByItems(
							items,
							secureContainer._id
						);

						// Remove child items + secure container
						info.profile.Inventory.items = items.filter((x) => !x?.parentId || childItemsInSecureContainer.includes(x._id));
					}

					info.exit = PlayerRaidEndState.SURVIVED;
				}

				return oldSaveProgress(url, info, sessionID);
			}
		}, {frequency: "Always"});
	}
}

module.exports = {mod: new Mod()}
